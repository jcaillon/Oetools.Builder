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
        
        private FileList<OeFileBuilt> _previouslyBuiltFiles;
        private OeBuildHistory _buildSourceHistory;

        public ILogger Log { protected get; set; }
      
        public OeBuildConfiguration BuildConfiguration { get; }

        public OeBuildHistory BuildSourceHistory {
            get => _buildSourceHistory;
            set {
                _previouslyBuiltFiles = null;
                _buildSourceHistory = value;
            }
        }

        public List<BuildStepExecutor> BuildStepExecutors { get; } = new List<BuildStepExecutor>();
        
        public List<TaskExecutionException> TaskExecutionExceptions => BuildStepExecutors
            .SelectMany(exec => (exec?.Tasks).ToNonNullList())
            .SelectMany(task => task.GetExceptionList().ToNonNullList())
            .ToList();

        private string BuildTemporaryDirectory { get; set; }

        private FileList<OeFileBuilt> PreviouslyBuiltFiles {
            get {
                if (_previouslyBuiltFiles == null && BuildSourceHistory?.BuiltFiles != null) {
                    _previouslyBuiltFiles = BuildSourceHistory.BuiltFiles.ToFileList();
                }
                return _previouslyBuiltFiles;
            }
        }

        protected bool UseIncrementalBuild => BuildConfiguration.Properties.IncrementalBuildOptions.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled();

        private bool StoreSourceHash => BuildConfiguration.Properties.IncrementalBuildOptions?.StoreSourceHash ?? OeIncrementalBuildOptions.GetDefaultStoreSourceHash();

        protected string SourceDirectory => BuildConfiguration.Properties.BuildOptions?.SourceDirectoryPath;
        
        private CancellationTokenSource CancelSource { get; } = new CancellationTokenSource();
        
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

        public void Dispose() {
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
            CancelSource.Token.Register(() => {
                Log?.Debug("Build cancel requested");
            });
            
            Log?.Debug($"Initializing build with {BuildConfiguration}");
            BuildConfiguration.Properties.SetCancellationSource(CancelSource);
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
        /// Call this method to cancel the whole build
        /// </summary>
        public void Cancel() {
            CancelSource.Cancel();
        }
        
        /// <summary>
        /// Executes the build
        /// </summary>
        private void ExecuteBuildConfiguration() {
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
            
            Log?.Debug($"Starting {executionName}");
            
            if (steps != null) {
                var stepsList = steps.ToList();
                var i = 0;
                foreach (var step in stepsList) {
                    Log?.Debug($"Starting {executionName} - {step}");
                    var executor = new T {
                        Name = executionName,
                        Id = i,
                        Tasks = (step.GetTaskList()?.Cast<IOeTask>()).ToNonNullList(),
                        Properties = BuildConfiguration.Properties,
                        Log = Log,
                        CancelSource = CancelSource
                    };
                    BuildStepExecutors.Add(executor);
                    executor.Configure();
                    if (executor is IBuildStepExecutorBuildSource buildSourceExecutor) {
                        buildSourceExecutor.PreviouslyBuiltFiles = PreviouslyBuiltFiles;
                        buildSourceExecutor.IsLastBuildStepExecutor = i == stepsList.Count - 1;
                        if (buildSourceExecutor.IsLastBuildStepExecutor) {
                            buildSourceExecutor.AllTasksOfAllSteps = stepsList.SelectMany(stp => stp.GetTaskList().ToNonNullList());
                        }
                    }
                    executor.Execute();
                    i++;
                }
            }
        }
        
        private OeBuildHistory GetBuildHistory() {
            var history = new OeBuildHistory {
                BuiltFiles = GetFilesBuiltHistory().ToList(),
                CompilationProblems = GetSourceCompilationProblems(),
                WebclientPackageInfo = null // TODO : webclient package info
            };
            return history;
        }

        /// <summary>
        /// List all the compilation problems of all the compile tasks
        /// </summary>
        /// <returns></returns>
        private List<OeCompilationProblem> GetSourceCompilationProblems() {
            var output = new List<OeCompilationProblem>();
            // add all compilation problems
            foreach (var file in BuildStepExecutors
                .Where(te => te is BuildStepExecutorWithFileListAndCompilation)
                .SelectMany(exec => (exec?.Tasks).ToNonNullList())
                .Where(task => task is IOeTaskCompile)
                .Cast<IOeTaskCompile>()
                .SelectMany(task => task.GetCompiledFiles().ToNonNullList())) {
                if (file.CompilationErrors == null || file.CompilationErrors.Count == 0) {
                    continue;
                }
                if (output.Exists(cp => cp.SourceFilePath.Equals(file.SourceFilePath, StringComparison.CurrentCultureIgnoreCase))) {
                    continue;
                }
                foreach (var problem in file.CompilationErrors) {
                    output.Add(OeCompilationProblem.New(file.SourceFilePath, problem));
                }
            }
            return output;
        }

        /// <summary>
        /// Returns a list of all the files built; include files that were automatically deleted
        /// </summary>
        /// <returns></returns>
        private FileList<OeFileBuilt> GetFilesBuiltHistory() {

            var builtFiles = new FileList<OeFileBuilt>();
            
            var outputDirectory = BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath;
            
            foreach (var fileBuilt in BuildStepExecutors
                .Where(te => te is BuildStepExecutorWithFileListAndCompilation)
                .Cast<BuildStepExecutorWithFileListAndCompilation>()
                .SelectMany(exec => (exec?.Tasks).ToNonNullList())
                .Where(t => t is IOeTaskFileBuilder)
                .Cast<IOeTaskFileBuilder>()
                .SelectMany(t => t.GetFilesBuilt().ToNonNullList())) {
                
                // keep only the targets that are in the output directory, we won't be able to "undo" the others so it would be useless to keep them
                var targetsOutputDirectory = fileBuilt.Targets
                    .Where(t => t.GetTargetPath().StartsWith(outputDirectory, StringComparison.CurrentCultureIgnoreCase))
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
            if (PreviouslyBuiltFiles != null) {
                foreach (var previousFile in PreviouslyBuiltFiles.Where(oldFile => oldFile.State != OeFileState.Deleted && !builtFiles.Contains(oldFile))) {
                    var previousFileCopy = previousFile.GetDeepCopy();
                    previousFileCopy.State = OeFileState.Unchanged;
                    builtFiles.Add(previousFile);
                }
            }
            
            // also ensures that all files have HASH info
            if (UseIncrementalBuild && StoreSourceHash) {
                foreach (var fileBuilt in builtFiles) {
                    if (fileBuilt.State != OeFileState.Deleted) {
                        SourceFilesLister.SetFileHash(fileBuilt);
                    }
                }
            }

            return builtFiles;
        }
        
    }
}