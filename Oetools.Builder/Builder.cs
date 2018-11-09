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
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder {
    
    public class Builder : IDisposable {
        
        private PathList<OeFileBuilt> _previouslyBuiltPaths;
        private OeBuildHistory _buildSourceHistory;

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

        private string BuildTemporaryDirectory { get; set; }

        private PathList<OeFileBuilt> PreviouslyBuiltPaths {
            get {
                if (_previouslyBuiltPaths == null && BuildSourceHistory?.BuiltFiles != null) {
                    _previouslyBuiltPaths = BuildSourceHistory.BuiltFiles.ToFileList();
                }
                return _previouslyBuiltPaths;
            }
        }

        protected bool UseIncrementalBuild => BuildConfiguration.Properties.IncrementalBuildOptions.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled();

        private bool StoreSourceHash => BuildConfiguration.Properties.IncrementalBuildOptions?.StoreSourceHash ?? OeIncrementalBuildOptions.GetDefaultStoreSourceHash();

        protected string SourceDirectory => BuildConfiguration.Properties.BuildOptions?.SourceDirectoryPath;

        public CancellationToken? CancelToken { get; set; }
        
        /// <summary>
        /// Initialize the build
        /// </summary>
        /// <param name="project"></param>
        /// <param name="buildConfigurationName"></param>
        public Builder(OeProject project, string buildConfigurationName = null) {
            // make a copy of the build configuration
            BuildConfiguration = project.GetBuildConfigurationCopy(buildConfigurationName) ?? project.GetDefaultBuildConfigurationCopy();
            ConstructorInitialization();
        }

        public Builder(OeBuildConfiguration buildConfiguration) {
            BuildConfiguration = buildConfiguration;
            ConstructorInitialization();
        }

        private void ConstructorInitialization() {
            BuildConfiguration.Properties = BuildConfiguration.Properties ?? new OeProperties();
            BuildConfiguration.Properties.SetDefaultValues();
            BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath = BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath ?? OeBuilderConstants.GetDefaultOutputDirectory(SourceDirectory);
        }

        public virtual void Dispose() {
            Utils.DeleteDirectoryIfExists(BuildTemporaryDirectory, true);
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
                    ExecuteBuildConfiguration();
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
            BuildTemporaryDirectory = BuildConfiguration.Properties.GetEnv().TempDirectory;
            
            Log?.Debug("Validating build configuration");
            BuildConfiguration.Validate();
            
            Log?.Debug("Using build variables");
            BuildConfiguration.ApplyVariables(SourceDirectory);
            
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
        private void ExecuteBuildConfiguration() {
            // compute the total number of tasks to execute
            TotalNumberOfTasks += BuildConfiguration.PreBuildStepGroup?.SelectMany(step => step.Tasks).Count() ?? 0;
            TotalNumberOfTasks += BuildConfiguration.BuildSourceStepGroup?.SelectMany(step => step.Tasks).Count() ?? 0;
            TotalNumberOfTasks += BuildConfiguration.BuildOutputStepGroup?.SelectMany(step => step.Tasks).Count() ?? 0;
            TotalNumberOfTasks += BuildConfiguration.PostBuildStepGroup?.SelectMany(step => step.Tasks).Count() ?? 0;
            TotalNumberOfTasks += PreviouslyBuiltPaths != null ? 2 : 0; // potential extra tasks for removal
            
            ExecuteBuildStep<BuildStepExecutor>(BuildConfiguration.PreBuildStepGroup, nameof(OeBuildConfiguration.PreBuildStepGroup));
            ExecuteBuildStep<BuildStepExecutorBuildSource>(BuildConfiguration.BuildSourceStepGroup, nameof(OeBuildConfiguration.BuildSourceStepGroup));
            ExecuteBuildStep<BuildStepExecutorBuildOutput>(BuildConfiguration.BuildOutputStepGroup, nameof(OeBuildConfiguration.BuildOutputStepGroup));
            ExecuteBuildStep<BuildStepExecutor>(BuildConfiguration.PostBuildStepGroup, nameof(OeBuildConfiguration.PostBuildStepGroup));
        }

        /// <summary>
        /// Executes a build step
        /// </summary>
        private void ExecuteBuildStep<T>(IEnumerable<OeBuildStep> steps, string oeBuildConfigurationPropertyName) where T : BuildStepExecutor, new() {
            var executionName = typeof(OeBuildConfiguration).GetXmlName(oeBuildConfigurationPropertyName);
            
            Log?.ReportGlobalProgress(TotalNumberOfTasks, NumberOfTasksDone, $"Executing {executionName}");

            if (steps == null) {
                return;
            }

            var stepsList = steps.ToList();
            var i = 0;
            foreach (var step in stepsList) {
                Log?.Info($"Starting {executionName} - {step}");
                var executor = new T {
                    Name = executionName,
                    Id = i,
                    Tasks = (step.GetTaskList()?.Cast<IOeTask>()).ToNonNullList(),
                    Properties = BuildConfiguration.Properties,
                    Log = Log,
                    CancelToken = CancelToken
                };
                executor.OnTaskStart += ExecutorOnOnTaskStart;
                BuildStepExecutors.Add(executor);
                ConfigureBuildSource(executor as BuildStepExecutorBuildSource, stepsList, i);
                executor.Execute();
                NumberOfTasksDone += executor.NumberOfTasksDone;
                i++;
            }
        }

        private void ExecutorOnOnTaskStart(object sender, StepExecutorProgressEventArgs e) {
            Log?.ReportGlobalProgress(TotalNumberOfTasks, TotalNumberOfTasks + e.NumberOfTasksDone, $"Starting task {e.CurrentTask}");
        }

        /// <summary>
        /// Configure the source build
        /// </summary>
        /// <param name="buildSourceExecutor"></param>
        /// <param name="stepsList"></param>
        /// <param name="currentStep"></param>
        private void ConfigureBuildSource(BuildStepExecutorBuildSource buildSourceExecutor, List<OeBuildStep> stepsList, int currentStep) {
            if (buildSourceExecutor == null) {
                return;
            }

            Log?.Debug("Is build source step");
            
            buildSourceExecutor.PreviouslyBuiltPaths = PreviouslyBuiltPaths;

            if (currentStep < stepsList.Count - 1) {
                return;
            }

            Log?.Debug("Is the last step");

            if (PreviouslyBuiltPaths == null) {
                return;
            }

            var tasksAdded = 0;
            
            var mirrorDeletedSourceFileToOutput = BuildConfiguration.Properties.IncrementalBuildOptions?.MirrorDeletedSourceFileToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput();
        
            var mirrorDeletedTargetsToOutput = BuildConfiguration.Properties.IncrementalBuildOptions?.MirrorDeletedTargetsToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedTargetsToOutput();

            if (mirrorDeletedSourceFileToOutput || mirrorDeletedTargetsToOutput) {
                Log?.Info("Mirroring deleted files in source to the output directory");

                Log?.Debug("Making a list of all the files that were deleted since the previous build (the targets of those files must be deleted");
                var filesWithTargetsToRemove = IncrementalBuildHelper.GetBuiltFilesDeletedSincePreviousBuild(PreviouslyBuiltPaths).ToFileList();
                if (filesWithTargetsToRemove != null && filesWithTargetsToRemove.Count > 0) {
                    var newTask = new OeTaskTargetsDeleter {
                        Label = "Deleting files missing from the previous build"
                    };
                    newTask.SetFilesWithTargetsToRemove(filesWithTargetsToRemove);
                    buildSourceExecutor.Tasks.Add(newTask);
                    tasksAdded++;
                }
            }

            if (mirrorDeletedTargetsToOutput) {
                Log?.Info("Mirroring deleted targets to the output directory");

                var unchangedOrModifiedFiles = buildSourceExecutor.SourceDirectoryCompletePathList?.CopyWhere(f => f.State == OeFileState.Unchanged || f.State == OeFileState.Modified);

                if (unchangedOrModifiedFiles != null && unchangedOrModifiedFiles.Count > 0) {
                    Log?.Debug("For all the unchanged or modified files, set all the targets that should be built in this build");
                    foreach (var task in stepsList.SelectMany(step1 => (step1.GetTaskList()?.OfType<IOeTaskFileWithTargets>()).ToNonNullList())) {
                        task.SetTargets(unchangedOrModifiedFiles, BuildConfiguration.Properties?.BuildOptions?.OutputDirectoryPath, true);
                    }

                    Log?.Debug("Making a list of all source files that had targets existing in the previous build which don't existing anymore (those targets must be deleted)");
                    var filesWithTargetsToRemove = IncrementalBuildHelper.GetBuiltFilesWithOldTargetsToRemove(unchangedOrModifiedFiles, PreviouslyBuiltPaths).ToFileList();

                    if (filesWithTargetsToRemove != null && filesWithTargetsToRemove.Count > 0) {
                        var newTask = new OeTaskTargetsDeleter {
                            Label = "Deleting previous targets that no longer exist"
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
                BuiltFiles = GetFilesBuiltHistory().ToList(),
                CompiledFiles = GetSourceCompilationProblems().ToList(),
                WebclientPackageInfo = null // TODO : webclient package info
            };
            return history;
        }

        /// <summary>
        /// List all the compilation problems of all the compile tasks
        /// </summary>
        /// <returns></returns>
        private PathList<OeCompiledFile> GetSourceCompilationProblems() {
            var output = new PathList<OeCompiledFile>();
            
            // add all compilation problems
            foreach (var file in BuildStepExecutors
                .Where(te => te is BuildStepExecutorBuildSource)
                .SelectMany(exec => exec.Tasks.ToNonNullList())
                .OfType<IOeTaskCompile>()
                .SelectMany(task => task.GetCompiledFiles().ToNonNullList())) {
                if (file.CompilationErrors == null || file.CompilationErrors.Count == 0) {
                    continue;
                }
                if (output.Contains(file.Path)) {
                    continue;
                }
                var compiledFile = new OeCompiledFile {
                    Path = file.Path,
                    CompilationProblems = new List<AOeCompilationProblem>()
                };
                foreach (var problem in file.CompilationErrors) {
                    compiledFile.CompilationProblems.Add(AOeCompilationProblem.New(problem));
                }
                output.Add(compiledFile);
            }
            
            return output;
        }

        /// <summary>
        /// Returns a list of all the files built; include files that were automatically deleted.
        /// </summary>
        /// <returns></returns>
        private PathList<OeFileBuilt> GetFilesBuiltHistory() {

            var builtFiles = new PathList<OeFileBuilt>();
            
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
                    builtFiles.Add(fileBuilt.GetDeepCopy());
                } else {
                    OeFileBuilt historyFileBuilt = builtFiles[fileBuilt];
                    if (historyFileBuilt.Targets == null) {
                        historyFileBuilt.Targets = targetsOutputDirectory;
                    } else {
                        historyFileBuilt.Targets.AddRange(targetsOutputDirectory);
                    }
                }
            }
            
            // add all the files that were not rebuild from the previous build history
            if (PreviouslyBuiltPaths != null) {
                foreach (var previousFile in PreviouslyBuiltPaths.Where(oldFile => oldFile.State != OeFileState.Deleted && !builtFiles.Contains(oldFile))) {
                    var previousFileCopy = previousFile.GetDeepCopy();
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