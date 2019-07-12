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
        private CancellationTokenRegistration? _cancelRegistration;

        /// <summary>
        /// Cancel token.
        /// </summary>
        public CancellationToken? CancelToken { get; set; }

        /// <summary>
        /// Logger.
        /// </summary>
        public ILogger Log { protected get; set; }

        /// <summary>
        /// The build configuration used for this build.
        /// </summary>
        public OeBuildConfiguration BuildConfiguration { get; }

        /// <summary>
        /// The previous build history to feed for this build and the build history to get once the build is complete.
        /// Only useful in incremental mode.
        /// </summary>
        public OeBuildHistory BuildSourceHistory { get; set; }

        /// <summary>
        /// A list of all the compilation problems encountered during the build.
        /// </summary>
        public List<AOeCompilationProblem> CompilationProblems => BuildStepExecutors
            .Where(te => te is BuildStepExecutorBuildSource)
            .SelectMany(exec => exec.Tasks.ToNonNullEnumerable())
            .OfType<IOeTaskCompile>()
            .SelectMany(task => task.GetCompiledFiles().ToNonNullEnumerable())
            .SelectMany(file => file.CompilationProblems.ToNonNullEnumerable())
            .Select(AOeCompilationProblem.New)
            .ToList();

        public PathList<IOeFileBuilt> GetAllFilesBuilt() => GetAllFilesBuiltMerged(te => true, task => !(task is AOeTaskTargetsRemover));

        public PathList<IOeFileBuilt> GetAllFilesWithTargetRemoved() => OeFileBuilt.MergeBuiltFilesTargets(BuildStepExecutors
            .SelectMany(exec => exec.Tasks.ToNonNullEnumerable())
            .OfType<AOeTaskTargetsRemover>()
            .SelectMany(t => t.GetRemovedTargets().ToNonNullEnumerable()));

        /// <summary>
        /// A list of all the task exceptions that occured during the build.
        /// It will contain all the ignored warnings/errors once the build is done.
        /// </summary>
        public List<TaskExecutionException> TaskExecutionExceptions => BuildStepExecutors?
            .SelectMany(exec => exec.TaskExecutionExceptions.ToNonNullEnumerable())
            .ToList();

        /// <summary>
        /// The list of step executors, to get access the the executed steps/tasks of this build.
        /// </summary>
        public List<BuildStepExecutor> BuildStepExecutors { get; } = new List<BuildStepExecutor>();

        private int TotalNumberOfTasks { get; set; }

        private int NumberOfTasksDone { get; set; }

        private PathList<IOeFileBuilt> PreviouslyBuiltPaths { get; set; }

        protected bool UseIncrementalBuild => BuildConfiguration.Properties.BuildOptions?.IncrementalBuildOptions?.EnabledIncrementalBuild ?? OeIncrementalBuildOptions.GetDefaultEnabledIncrementalBuild();

        private bool StoreSourceHash => BuildConfiguration.Properties.BuildOptions?.IncrementalBuildOptions?.UseCheckSumComparison ?? OeIncrementalBuildOptions.GetDefaultUseCheckSumComparison();

        /// <summary>
        /// Initialize the build.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="buildConfigurationName"></param>
        public Builder(OeProject project, string buildConfigurationName = null) {
            // make a copy of the build configuration
            BuildConfiguration = project.GetConfiguration(buildConfigurationName) ?? project.GetDefaultBuildConfigurationCopy();
            BuildConfiguration.SetDefaultValues();
        }

        /// <summary>
        /// Initialize the build.
        /// </summary>
        /// <param name="buildConfiguration"></param>
        public Builder(OeBuildConfiguration buildConfiguration) {
            BuildConfiguration = buildConfiguration;
            BuildConfiguration.SetDefaultValues();
        }

        public virtual void Dispose() {
            BuildConfiguration.Properties.GetEnv().Dispose();
            _cancelRegistration?.Dispose();
        }

        /// <summary>
        /// Starts the build process.
        /// </summary>
        /// <exception cref="BuilderException"></exception>
        public void Build() {
            try {
                PreBuild();
                try {
                    Log?.Info($"Starting build for {BuildConfiguration.ToString().PrettyQuote()}.");
                    if (UseIncrementalBuild) {
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
            _cancelRegistration = CancelToken?.Register(() => {
                Log?.Warn("Build cancel requested.");
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

            if (BuildSourceHistory?.BuiltFiles != null) {
                PreviouslyBuiltPaths = BuildSourceHistory.BuiltFiles.OfType<IOeFileBuilt>().ToFileList();
            }
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

            if (BuildConfiguration.BuildSteps != null) {

                foreach (var step in BuildConfiguration.BuildSteps) {
                    Log?.Info($"Executing {step.ToString().PrettyQuote()}.");

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
                        buildSourceExecutor.GetFilesToBuildFromSourceFiles = GetFilesToBuildFromSourceFiles;
                    }

                    executor.OnTaskStart += ExecutorOnOnTaskStart;
                    executor.Execute();
                    executor.OnTaskStart -= ExecutorOnOnTaskStart;

                    NumberOfTasksDone += executor.NumberOfTasksDone;
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
            Log?.ReportGlobalProgress(TotalNumberOfTasks, NumberOfTasksDone + e.NumberOfTasksDone, $"Executing {e.CurrentTask.PrettyQuote()}.");
        }

        /// <summary>
        /// Gets a list of all the files to build for this build from a list of source files.
        /// </summary>
        /// <param name="sourceFiles"></param>
        /// <returns></returns>
        internal PathList<IOeFileToBuild> GetFilesToBuildFromSourceFiles(IEnumerable<IOeFile> sourceFiles) {
            var unchangedOrModifiedFiles = OeFile.ConvertToFileToBuild(sourceFiles);

            if (unchangedOrModifiedFiles != null) {
                foreach (var unchangedOrModifiedFile in unchangedOrModifiedFiles) {
                    unchangedOrModifiedFile.TargetsToBuild = null;
                }

                if (unchangedOrModifiedFiles.Count > 0) {
                    foreach (var task in BuildConfiguration.BuildSteps.OfType<OeBuildStepBuildSource>().SelectMany(step => (step.Tasks?.OfType<IOeTaskFileToBuild>()).ToNonNullEnumerable())) {
                        // for all the task that build files.
                        task.SetTargets(unchangedOrModifiedFiles.CopyWhere(f => task.IsPathPassingFilter(f.Path)), BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, true);
                    }
                }
            }

            return unchangedOrModifiedFiles;
        }

        /// <summary>
        /// Returns a new build history.
        /// </summary>
        /// <returns></returns>
        private OeBuildHistory GetBuildHistory() {
            if (!UseIncrementalBuild) {
                return null;
            }
            var history = new OeBuildHistory {
                BuiltFiles = GetBuiltFilesHistory().Select(f => {
                    if (f is OeFileBuilt fb) {
                        return fb;
                    }
                    return new OeFileBuilt(f);
                }).ToList(),
                WebclientPackageInfo = null // TODO : webclient package info
            };
            return history;
        }

        private PathList<IOeFileBuilt> GetAllFilesBuiltMerged(Func<BuildStepExecutor, bool> stepSelector, Func<IOeTask, bool> taskSelector) {
            return OeFileBuilt.MergeBuiltFilesTargets(BuildStepExecutors
                .Where(stepSelector)
                .SelectMany(exec => exec.Tasks.ToNonNullEnumerable())
                .Where(taskSelector)
                .OfType<IOeTaskWithBuiltFiles>()
                .SelectMany(t => t.GetBuiltFiles().ToNonNullEnumerable()));
        }

        /// <summary>
        /// Returns a list of all the files built.
        /// </summary>
        /// <returns></returns>
        private PathList<IOeFileBuilt> GetBuiltFilesHistory() {
            var sourceDirectoryCompletePathList = BuildStepExecutors.OfType<BuildStepExecutorBuildSource>().LastOrDefault()?.SourceDirectoryCompletePathList;
            if (sourceDirectoryCompletePathList == null) {
                return new PathList<IOeFileBuilt>();
            }

            // add all the source files built.
            var builtFiles = GetAllFilesBuiltMerged(te => te is BuildStepExecutorBuildSource, task => !(task is AOeTaskTargetsRemover));

            // add unchanged files for which we deleted targets.
            foreach (var fileBuilt in BuildStepExecutors
                .SelectMany(exec => exec.Tasks.ToNonNullEnumerable())
                .OfType<OeTaskReflectDeletedTargets>()
                .SelectMany(t => t.GetBuiltFiles().ToNonNullEnumerable())) {
                // if the built list already contains this file, it means it has been rebuilt so it already has the right targets.
                if (!builtFiles.Contains(fileBuilt)) {
                    builtFiles.Add(new OeFileBuilt(fileBuilt));
                }
            }

            // add all the files that were not rebuild from the previous build history.
            if (PreviouslyBuiltPaths != null) {
                foreach (var previousFile in PreviouslyBuiltPaths
                    .Where(oldFile => !builtFiles.Contains(oldFile) && sourceDirectoryCompletePathList.Contains(oldFile.Path))) {
                    var previousFileCopy = new OeFileBuilt(previousFile) {
                        State = OeFileState.Unchanged
                    };
                    builtFiles.TryAdd(previousFileCopy);
                }
            }

            // finally, add all the required files.
            foreach (var requiredFile in builtFiles.SelectMany(f => f.RequiredFiles.ToNonNullEnumerable()).Where(reqFile => !builtFiles.Contains(reqFile)).ToList()) {
                var sourceFileRequired = sourceDirectoryCompletePathList[requiredFile];
                if (sourceFileRequired == null) {
                    continue;
                }
                builtFiles.TryAdd(new OeFileBuilt(sourceFileRequired));
            }

            // also ensures that all files have HASH info.
            if (UseIncrementalBuild && StoreSourceHash) {
                foreach (var fileBuilt in builtFiles) {
                    PathLister.SetFileHash(fileBuilt);
                }
            }

            return builtFiles;
        }

    }
}
