#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (Builder.cs) is part of Oetools.Builder.
// 
// Oetools.Builder is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder {
    
    /// <summary>
    /// A builder to build an openedge project.
    /// </summary>
    public class Builder : IDisposable {
        
        private PathList<IOeFileBuilt> _previouslyBuiltPaths;
        private OeBuildHistory _buildSourceHistory;
        
        public CancellationToken? CancelToken { get; set; }

        public ILogger Log { protected get; set; }
      
        public OeBuildConfiguration BuildConfiguration { get; }

        public OeBuildHistory BuildSourceHistory {
            get => _buildSourceHistory;
            set {
                _previouslyBuiltPaths = null;
                _buildSourceHistory = value;
            }
        }

        public List<BuildStepExecutor> BuildStepExecutors { get; } = new List<BuildStepExecutor>();
        
        public List<TaskExecutionException> TaskExecutionExceptions => BuildStepExecutors
            .SelectMany(exec => (exec?.Tasks).ToNonNullList())
            .SelectMany(task => task.GetRuntimeExceptionList().ToNonNullList())
            .ToList();
        
        private int TotalNumberOfTasks { get; set; }
        
        private int NumberOfTasksDone { get; set; }

        private PathList<IOeFileBuilt> PreviouslyBuiltPaths {
            get {
                if (_previouslyBuiltPaths == null && BuildSourceHistory?.BuiltFiles != null) {
                    _previouslyBuiltPaths = BuildSourceHistory.BuiltFiles.OfType<IOeFileBuilt>().ToFileList();
                }
                return _previouslyBuiltPaths;
            }
        }

        protected bool UseIncrementalBuild => BuildConfiguration.Properties.BuildOptions?.IncrementalBuildOptions?.EnabledIncrementalBuild ?? OeIncrementalBuildOptions.GetDefaultEnabledIncrementalBuild();

        private bool StoreSourceHash => BuildConfiguration.Properties.BuildOptions?.IncrementalBuildOptions?.UseCheckSumComparison ?? OeIncrementalBuildOptions.GetDefaultUseCheckSumComparison();

        /// <summary>
        /// Initialize the build
        /// </summary>
        /// <param name="project"></param>
        /// <param name="buildConfigurationName"></param>
        public Builder(OeProject project, string buildConfigurationName = null) {
            // make a copy of the build configuration
            BuildConfiguration = project.GetBuildConfigurationCopy(buildConfigurationName) ?? project.GetDefaultBuildConfigurationCopy();
            BuildConfiguration.SetDefaultValues();
        }

        public Builder(OeBuildConfiguration buildConfiguration) {
            BuildConfiguration = buildConfiguration;
            BuildConfiguration.SetDefaultValues();
        }

        public virtual void Dispose() {
            BuildConfiguration.Properties.GetEnv().Dispose();
        }
        
        /// <summary>
        /// Main method, builds
        /// </summary>
        /// <exception cref="BuilderException"></exception>
        public void Build() {
            try {
                PreBuild();
                try {
                    Log?.Info($"Start building {BuildConfiguration}");
                    ExecuteBuildSteps();
                } catch (OperationCanceledException) {
                    Log?.Debug("Build canceled");
                    throw;
                }
                PostBuild();
            } catch (Exception e) {
                throw new BuilderException(e.Message, e);
            }
        }

        protected virtual void PreBuild() {    
            CancelToken?.Register(() => {
                Log?.Debug("Build cancel requested");
            });
            
            Log?.Debug($"Initializing build with {BuildConfiguration}");
            BuildConfiguration.Properties.SetCancellationSource(CancelToken);
            
            Log?.Debug("Validating build configuration");
            BuildConfiguration.Validate();
            
            Log?.Debug("Using build variables");
            BuildConfiguration.ApplyVariables();
            
            Log?.Debug("Sanitizing path properties");
            BuildConfiguration.Properties.SanitizePathInPublicProperties();
        }

        protected virtual void PostBuild() {
            if (UseIncrementalBuild) {
                BuildSourceHistory = GetBuildHistory();
            }
        }
        
        /// <summary>
        /// Executes the build
        /// </summary>
        private void ExecuteBuildSteps() {
            // compute the total number of tasks to execute
            TotalNumberOfTasks += BuildConfiguration.BuildSteps?.SelectMany(step => step.Tasks).Count() ?? 0;
            TotalNumberOfTasks += PreviouslyBuiltPaths != null ? 2 : 0; // potential extra tasks for removal
            
            Log?.ReportGlobalProgress(TotalNumberOfTasks, NumberOfTasksDone, "Starting step execution.");
            if (BuildConfiguration.BuildSteps != null) {
                var buildSourceStepList = BuildConfiguration.BuildSteps.OfType<OeBuildStepBuildSource>().ToList();
                var buildSourceStepCount = 0;
                
                foreach (var step in BuildConfiguration.BuildSteps) {
                    Log?.Info($"Executing {step}");
                    
                    BuildStepExecutor executor;
                    switch (step) {
                        case OeBuildStepBuildSource _:
                            executor = new BuildStepExecutorBuildSource();
                            break;
                        case OeBuildStepBuildOutput _:
                            executor = new BuildStepExecutorBuildOutput();
                            break;
                        default:
                            executor = new BuildStepExecutor();
                            break;
                    }

                    executor.Name = step.ToString();
                    executor.Tasks = step.Tasks.ToNonNullEnumerable().OfType<IOeTask>().ToList();
                    executor.Properties = BuildConfiguration.Properties;
                    executor.Log = Log;
                    executor.CancelToken = CancelToken;
                    
                    BuildStepExecutors.Add(executor);
                    
                    if (executor is BuildStepExecutorBuildSource buildSourceExecutor) {
                        Log?.Debug("Is build source step.");
                        buildSourceExecutor.PreviouslyBuiltPaths = PreviouslyBuiltPaths;
                        if (buildSourceStepCount == buildSourceStepList.Count - 1) {
                            Log?.Debug("Is the last build source step.");
                            AddRemovalTasksToBuildSourceStep(buildSourceExecutor, buildSourceStepList);
                        }
                        buildSourceStepCount++;
                    }
                    
                    executor.OnTaskStart += ExecutorOnOnTaskStart;
                    executor.Execute();
                    executor.OnTaskStart -= ExecutorOnOnTaskStart;
                    NumberOfTasksDone += executor.NumberOfTasksDone;
                }
            }
            Log?.ReportGlobalProgress(TotalNumberOfTasks, TotalNumberOfTasks, "Ending step execution.");
        }

        private void ExecutorOnOnTaskStart(object sender, StepExecutorProgressEventArgs e) {
            Log?.ReportGlobalProgress(TotalNumberOfTasks, TotalNumberOfTasks + e.NumberOfTasksDone, $"Executing {e.CurrentTask}");
        }

        /// <summary>
        /// Allow to delete obsolete files/targets there were previously built.
        /// </summary>
        /// <param name="buildSourceExecutor"></param>
        /// <param name="stepsList"></param>
        private void AddRemovalTasksToBuildSourceStep(BuildStepExecutorBuildSource buildSourceExecutor, List<OeBuildStepBuildSource> stepsList) {
            if (PreviouslyBuiltPaths == null) {
                return;
            }

            var tasksAdded = 0;
            
            var mirrorDeletedSourceFileToOutput = BuildConfiguration.Properties.BuildOptions?.IncrementalBuildOptions?.MirrorDeletedSourceFileToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput();
        
            var mirrorDeletedTargetsToOutput = BuildConfiguration.Properties.BuildOptions?.IncrementalBuildOptions?.MirrorDeletedTargetsToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedTargetsToOutput();

            if (mirrorDeletedSourceFileToOutput || mirrorDeletedTargetsToOutput) {
                Log?.Info("Mirroring deleted files in source to the output directory");

                Log?.Debug("Making a list of all the files that were deleted since the previous build (the targets of those files must be deleted");
                var filesWithTargetsToRemove = IncrementalBuildHelper.GetBuiltFilesDeletedSincePreviousBuild(PreviouslyBuiltPaths).ToFileList();
                if (filesWithTargetsToRemove != null && filesWithTargetsToRemove.Count > 0) {
                    var newTask = new OeTaskTargetsDeleter {
                        Name = "Deleting files missing from the previous build"
                    };
                    newTask.SetFilesWithTargetsToRemove(filesWithTargetsToRemove);
                    buildSourceExecutor.Tasks.Add(newTask);
                    tasksAdded++;
                }
            }

            if (mirrorDeletedTargetsToOutput) {
                Log?.Info("Mirroring deleted targets to the output directory");

                var unchangedOrModifiedFiles = OeFile.ConvertToFileToBuild(buildSourceExecutor.SourceDirectoryCompletePathList?.Where(f => f.State == OeFileState.Unchanged || f.State == OeFileState.Modified));

                if (unchangedOrModifiedFiles != null && unchangedOrModifiedFiles.Count > 0) {
                    Log?.Debug("For all the unchanged or modified files, set all the targets that should be built in this build");
                    foreach (var task in stepsList.SelectMany(step1 => (step1.Tasks?.OfType<IOeTaskFileToBuild>()).ToNonNullList())) {
                        task.SetTargets(unchangedOrModifiedFiles, BuildConfiguration.Properties?.BuildOptions?.OutputDirectoryPath, true);
                    }

                    Log?.Debug("Making a list of all source files that had targets existing in the previous build which don't existing anymore (those targets must be deleted)");
                    var filesWithTargetsToRemove = IncrementalBuildHelper.GetBuiltFilesWithOldTargetsToRemove(unchangedOrModifiedFiles, PreviouslyBuiltPaths).ToFileList();

                    if (filesWithTargetsToRemove != null && filesWithTargetsToRemove.Count > 0) {
                        var newTask = new OeTaskTargetsDeleter {
                            Name = "Deleting previous targets that no longer exist"
                        };
                        newTask.SetFilesWithTargetsToRemove(filesWithTargetsToRemove);
                        buildSourceExecutor.Tasks.Add(newTask);
                        tasksAdded++;
                    }
                }
            }

            TotalNumberOfTasks -= 2 - tasksAdded;
        }
        
        private OeBuildHistory GetBuildHistory() {
            var history = new OeBuildHistory {
                BuiltFiles = GetFilesBuiltHistory().Select(f => {
                    if (f is OeFileBuilt fb) {
                        return fb;
                    }
                    return new OeFileBuilt(f);
                }).ToList(),
                WebclientPackageInfo = null // TODO : webclient package info
            };
            return history;
        }

        /// <summary>
        /// Returns a list of all the files built; include files that were automatically deleted.
        /// </summary>
        /// <returns></returns>
        private PathList<IOeFileBuilt> GetFilesBuiltHistory() {

            var builtFiles = new PathList<IOeFileBuilt>();
            
            var outputDirectory = BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath;
            
            foreach (var fileBuilt in BuildStepExecutors
                .Where(te => te is BuildStepExecutorBuildSource)
                .SelectMany(exec => exec.Tasks.ToNonNullList())
                .OfType<IOeTaskWithBuiltFiles>()
                .SelectMany(t => t.GetBuiltFiles().ToNonNullList())) {
                
                // keep only the targets that are in the output directory, we won't be able to "undo" the others so it would be useless to keep them
                var targetsOutputDirectory = fileBuilt.Targets
                    .Where(t => t.GetTargetPath().StartsWith(outputDirectory, StringComparison.Ordinal))
                    .ToList();
                
                if (!builtFiles.Contains(fileBuilt)) {
                    builtFiles.Add((IOeFileBuilt) fileBuilt.DeepCopyToNew(fileBuilt.GetType()));
                } else {
                    var historyFileBuilt = builtFiles[fileBuilt];
                    if (historyFileBuilt.Targets == null) {
                        historyFileBuilt.Targets = targetsOutputDirectory;
                    } else {
                        historyFileBuilt.Targets.AddRange(targetsOutputDirectory);
                    }
                }
            }
            
            // add all compilation problems
            foreach (var file in BuildStepExecutors
                .Where(te => te is BuildStepExecutorBuildSource)
                .SelectMany(exec => exec.Tasks.ToNonNullList())
                .OfType<IOeTaskCompile>()
                .SelectMany(task => task.GetCompiledFiles().ToNonNullList())) {
                if (file.CompilationProblems == null || file.CompilationProblems.Count == 0) {
                    continue;
                }
                if (!builtFiles.Contains(file.Path)) {
                    var builtFileCompiled = new OeFileBuiltCompiled {
                        Path = file.Path,
                        Size = -1,
                        State = OeFileState.Added,
                        CompilationProblems = new List<AOeCompilationProblem>()
                    };
                    foreach (var compilationProblem in file.CompilationProblems) {
                        builtFileCompiled.CompilationProblems.Add(AOeCompilationProblem.New(compilationProblem));
                    }
                    builtFiles.Add(builtFileCompiled);
                }
                if (builtFiles[file.Path] is OeFileBuiltCompiled compiledFile) {
                    compiledFile.CompilationProblems = new List<AOeCompilationProblem>();
                    foreach (var problem in file.CompilationProblems) {
                        compiledFile.CompilationProblems.Add(AOeCompilationProblem.New(problem));
                    }
                } else {
                    Log?.Error($"Compilation problems found for a file which does not seem to have been compiled: {file.Path.PrettyQuote()}");
                }
            }
            
            // add all the files that were not rebuild from the previous build history
            if (PreviouslyBuiltPaths != null) {
                foreach (var previousFile in PreviouslyBuiltPaths.Where(oldFile => oldFile.State != OeFileState.Deleted && !builtFiles.Contains(oldFile))) {
                    var previousFileCopy = (IOeFileBuilt) previousFile.DeepCopyToNew(previousFile.GetType());
                    previousFileCopy.State = OeFileState.Unchanged;
                    builtFiles.Add(previousFileCopy);
                }
            }
            
            // also ensures that all files have HASH info
            if (UseIncrementalBuild && StoreSourceHash) {
                foreach (var fileBuilt in builtFiles) {
                    if (fileBuilt.State != OeFileState.Deleted) {
                        PathLister.SetFileHash(fileBuilt);
                    }
                }
            }

            return builtFiles;
        }
        
    }
}