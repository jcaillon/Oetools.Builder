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
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
    
    /// <summary>
    /// A builder to build an openedge project.
    /// </summary>
    public class Builder : IDisposable {
        
        private PathList<IOeFileBuilt> _previouslyBuiltPaths;
        private OeBuildHistory _buildSourceHistory;
        private int _stepDoneCount;

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
            .SelectMany(exec => exec.TaskExecutionExceptions.ToNonNullEnumerable())
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
                    Log?.Info($"Starting build for {BuildConfiguration.ToString().PrettyQuote()}.");
                    if (BuildConfiguration.Properties.BuildOptions?.IncrementalBuildOptions?.EnabledIncrementalBuild ?? OeIncrementalBuildOptions.GetDefaultEnabledIncrementalBuild()) {
                        Log?.Info("Incremental build enabled (differential from last build).");
                    }
                    ExecuteBuildSteps();
                } catch (OperationCanceledException) {
                    Log?.Debug("Build canceled.");
                    throw;
                }
                PostBuild();
            } catch (Exception e) {
                throw new BuilderException(e.Message, e);
            }
        }

        /// <summary>
        /// Executed before the build start.
        /// </summary>
        protected virtual void PreBuild() {    
            CancelToken?.Register(() => {
                Log?.Debug("Build cancel requested.");
            });
            
            Log?.Debug($"Initializing build with {BuildConfiguration}.");
            BuildConfiguration.Properties.SetCancellationSource(CancelToken);
            
            Log?.Debug("Validating build configuration.");
            BuildConfiguration.Validate();
            
            Log?.Debug("Using build variables.");
            BuildConfiguration.ApplyVariables();
            
            Log?.Debug("Sanitizing path properties.");
            BuildConfiguration.Properties.SanitizePathInPublicProperties();
            
            Log?.Debug("Computing the propath.");
            BuildConfiguration.Properties.SetPropathEntries();
        }

        /// <summary>
        /// Executed after the build has ended.
        /// </summary>
        protected virtual void PostBuild() {
            BuildSourceHistory = GetBuildHistory();
        }
        
        /// <summary>
        /// Executes the build.
        /// </summary>
        private void ExecuteBuildSteps() {
            // compute the total number of tasks to execute
            TotalNumberOfTasks += BuildConfiguration.BuildSteps?.SelectMany(step => step.Tasks.ToNonNullEnumerable()).Count() ?? 0;
            TotalNumberOfTasks += PreviouslyBuiltPaths != null ? 2 : 0; // potential extra tasks for removal
            
            if (BuildConfiguration.BuildSteps != null) {
                var buildSourceStepList = BuildConfiguration.BuildSteps.OfType<OeBuildStepBuildSource>().ToList();
                var buildSourceStepCount = 0;
                _stepDoneCount = 0;
                
                foreach (var step in BuildConfiguration.BuildSteps) {
                    Log?.Info($"{(_stepDoneCount == BuildConfiguration.BuildSteps.Count - 1 ? "└─ " : "├─ ")}Executing {step.ToString().PrettyQuote()}.");
                    
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
                        buildSourceStepCount++;
                        buildSourceExecutor.PreviouslyBuiltPaths = PreviouslyBuiltPaths;
                        buildSourceExecutor.IsLastSourceExecutor = buildSourceStepCount == buildSourceStepList.Count;
                        buildSourceExecutor.IsFirstSourceExecutor = buildSourceStepCount == 1;
                        buildSourceExecutor.StepsList = buildSourceStepList;
                    }
                    
                    executor.OnTaskStart += ExecutorOnOnTaskStart;
                    executor.Execute();
                    executor.OnTaskStart -= ExecutorOnOnTaskStart;

                    if (buildSourceStepCount == buildSourceStepList.Count) {
                        // recompute the real number of tasks
                        TotalNumberOfTasks += BuildConfiguration.BuildSteps.SelectMany(s => s.Tasks.ToNonNullEnumerable()).Count();
                    }
                    
                    NumberOfTasksDone += executor.NumberOfTasksDone;
                    _stepDoneCount++;
                }
            }
            Log?.ReportGlobalProgress(TotalNumberOfTasks, TotalNumberOfTasks, "Ending step execution.");
        }

        /// <summary>
        /// Called before a task starts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExecutorOnOnTaskStart(object sender, StepExecutorProgressEventArgs e) {
            Log?.ReportGlobalProgress(TotalNumberOfTasks, NumberOfTasksDone + e.NumberOfTasksDone, $"{(_stepDoneCount == BuildConfiguration.BuildSteps.Count - 1 ? "   " : "│  ")}{(e.NumberOfTasksDone == e.TotalNumberOfTasks - 1 ? "└─ " : "├─ ")}Executing {e.CurrentTask.PrettyQuote()}.");
        }
        
        /// <summary>
        /// Returns a new build history.
        /// </summary>
        /// <returns></returns>
        protected OeBuildHistory GetBuildHistory() {
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
            
            // add all the built files
            foreach (var fileBuilt in BuildStepExecutors
                .Where(te => te is BuildStepExecutorBuildSource)
                .SelectMany(exec => exec.Tasks.ToNonNullEnumerable())
                .OfType<IOeTaskWithBuiltFiles>()
                .SelectMany(t => t.GetBuiltFiles().ToNonNullEnumerable())) {
                
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
                .SelectMany(exec => exec.Tasks.ToNonNullEnumerable())
                .OfType<IOeTaskCompile>()
                .SelectMany(task => task.GetCompiledFiles().ToNonNullEnumerable())) {
                if (file.CompilationProblems == null || file.CompilationProblems.Count == 0) {
                    continue;
                }
                if (!builtFiles.Contains(file.Path)) {
                    // not yet in the built files list because it was not compiled successfully
                    OeFileBuilt fileCompiledWithError;
                    var sourceFileCompiled = BuildStepExecutors.OfType<BuildStepExecutorBuildSource>().LastOrDefault()?.SourceDirectoryCompletePathList[file.Path];
                    if (sourceFileCompiled == null) {
                        // for git filters, this include file is not listed in SourceDirectoryCompletePathList.
                        fileCompiledWithError = new OeFileBuilt {
                            Path = file.Path, 
                            State = OeFileState.Added
                        };
                        PathLister.SetFileBaseInfo(fileCompiledWithError);
                    } else {
                        fileCompiledWithError = new OeFileBuilt(sourceFileCompiled);
                    }
                    fileCompiledWithError.RequiredFiles = file.RequiredFiles?.ToList();
                    fileCompiledWithError.RequiredDatabaseReferences = file.RequiredDatabaseReferences?.Select(OeDatabaseReference.New).ToList();
                    builtFiles.Add(fileCompiledWithError);
                }
                if (builtFiles[file.Path] is OeFileBuilt compiledFile) {
                    compiledFile.CompilationProblems = new List<AOeCompilationProblem>();
                    foreach (var problem in file.CompilationProblems) {
                        compiledFile.CompilationProblems.Add(AOeCompilationProblem.New(problem));
                    }
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
            
            // finally, add all the required files
            var lastBuildSourceExecutor = BuildStepExecutors.OfType<BuildStepExecutorBuildSource>().LastOrDefault();
            if (lastBuildSourceExecutor != null) {
                foreach (var fileBuilt in builtFiles.Where(f => f.RequiredFiles != null).ToList()) {
                    foreach (var requiredFile in fileBuilt.RequiredFiles) {
                        if (!builtFiles.Contains(requiredFile)) {
                            var sourceFileRequired = lastBuildSourceExecutor.SourceDirectoryCompletePathList[requiredFile];
                            if (sourceFileRequired != null) {
                                builtFiles.Add(new OeFileBuilt(sourceFileRequired));
                            } else if (requiredFile.StartsWith(BuildConfiguration.Properties.BuildOptions.SourceDirectoryPath, StringComparison.OrdinalIgnoreCase)) {
                                // for git filters, this include file is not listed in SourceDirectoryCompletePathList.
                                var newRequiredFile = new OeFileBuilt {
                                    Path = requiredFile, 
                                    State = OeFileState.Added
                                };
                                PathLister.SetFileBaseInfo(newRequiredFile);
                                builtFiles.Add(newRequiredFile);
                            }
                        }
                    }
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