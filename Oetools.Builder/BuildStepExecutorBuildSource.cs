using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotUtilities;
using DotUtilities.Extensions;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {

    public class BuildStepExecutorBuildSource : BuildStepExecutor {

        /// <inheritdoc />
        protected override string BaseTargetDirectory => Properties?.BuildOptions?.OutputDirectoryPath;

        /// <summary>
        /// List of previously built paths.
        /// </summary>
        internal PathList<IOeFileBuilt> PreviouslyBuiltPaths { private get; set; }

        /// <summary>
        /// Function to return all the files that should be built from the given source files for all the tasks of this build.
        /// </summary>
        internal Func<IEnumerable<IOeFile>, PathList<IOeFileToBuild>> GetFilesToBuildFromSourceFiles { get; set; }

        private bool UseIncrementalBuild => Properties?.BuildOptions?.IncrementalBuildOptions?.EnabledIncrementalBuild ?? OeIncrementalBuildOptions.GetDefaultEnabledIncrementalBuild();

        private string SourceDirectory => (Properties?.BuildOptions?.SourceDirectoryPath).TakeDefaultIfNeeded(OeBuildOptions.GetDefaultSourceDirectoryPath());

        private bool FullRebuild => Properties?.BuildOptions?.FullRebuild ?? OeBuildOptions.GetDefaultFullRebuild();

        private PathList<IOeFile> _sourceDirectoryPathListToBuild;

        private PathList<IOeFile> _sourceDirectoryCompletePathList;

        /// <inheritdoc />
        protected override void ExecuteInternal() {
            IDisposable compiler = null;
            if (!TestMode) {
                Log?.Debug("Compiling files from all tasks.");
                compiler = CompileFiles(out var compiledPath);
                Log?.Debug("Associate the list of compiled files for each task.");
                foreach (var task in Tasks) {
                    if (task is IOeTaskCompile taskCompile) {
                        taskCompile.SetCompiledFiles(compiledPath?.CopyWhere(cf => taskCompile.GetFilesToProcess().Contains(cf.Path)));
                    }
                }
            }
            try {
                base.ExecuteInternal();
            } finally {
                compiler?.Dispose();
            }
        }

        /// <inheritdoc />
        protected override void InjectPropertiesInTask(IOeTask task) {
            base.InjectPropertiesInTask(task);
            if (task is OeTaskReflectDeletedSourceFile taskReflectDeletedSourceFile) {
                taskReflectDeletedSourceFile.SetFilesWithTargetsToRemove(null);
                if (PreviouslyBuiltPaths != null) {
                    Log?.Debug("Configuring removal task for previous files not existing anymore.");
                    var filesWithTargetsToRemove = IncrementalBuildHelper.GetBuiltFilesDeletedSincePreviousBuild(SourceDirectoryCompletePathList, PreviouslyBuiltPaths).ToFileList();
                    taskReflectDeletedSourceFile.SetFilesWithTargetsToRemove(filesWithTargetsToRemove);
                    if (filesWithTargetsToRemove != null && filesWithTargetsToRemove.Count > 0) {
                        Log?.Debug($"{filesWithTargetsToRemove.Count} files were deleted since the previous build (their targets will be deleted).");
                    }
                }
            }
            if (task is OeTaskReflectDeletedTargets taskReflectDeletedTargets) {
                taskReflectDeletedTargets.SetFilesWithTargetsToRemove(null);
                taskReflectDeletedTargets.SetFilesBuilt(null);
                if (PreviouslyBuiltPaths != null) {
                    Log?.Debug("Configuring removal task for previous targets not existing anymore.");
                    var unchangedOrModifiedFilesToBuild = GetFilesToBuildFromSourceFiles(SourceDirectoryCompletePathList?.Where(f => f.State == OeFileState.Unchanged || f.State == OeFileState.Modified));
                    if (unchangedOrModifiedFilesToBuild != null && unchangedOrModifiedFilesToBuild.Count > 0) {
                        var filesWithTargetsToRemove = IncrementalBuildHelper.GetBuiltFilesWithOldTargetsToRemove(unchangedOrModifiedFilesToBuild, PreviouslyBuiltPaths, out PathList<IOeFileBuilt> previousFilesBuiltUnchangedWithUpdatedTargets).ToFileList();
                        if (filesWithTargetsToRemove != null && filesWithTargetsToRemove.Count > 0) {
                            taskReflectDeletedTargets.SetFilesWithTargetsToRemove(filesWithTargetsToRemove);
                            taskReflectDeletedTargets.SetFilesBuilt(previousFilesBuiltUnchangedWithUpdatedTargets);
                            Log?.Debug($"{filesWithTargetsToRemove.Count} files that had targets existing in the previous build which don't existing anymore (their targets will be deleted).");
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override PathList<IOeFile> GetFilesToBuildForSingleTask(IOeTaskFile task) {
            Log?.Debug($"Getting the list of files to build for task: {task.ToString().PrettyQuote()}.");

            var baseList = SourceDirectoryPathListToBuild;

            if (UseIncrementalBuild && !FullRebuild ) {
                if (task is IOeTaskFileToBuild taskTarget) {
                    var rebuildFilesWithNewTargets = Properties?.BuildOptions?.IncrementalBuildOptions?.RebuildFilesWithNewTargets ?? OeIncrementalBuildOptions.GetDefaultRebuildFilesWithNewTargets();
                    var rebuildFilesWithMissingTargets = Properties?.BuildOptions?.IncrementalBuildOptions?.RebuildFilesWithMissingTargets ?? OeIncrementalBuildOptions.GetDefaultRebuildFilesWithMissingTargets();

                    if (rebuildFilesWithNewTargets || rebuildFilesWithMissingTargets) {
                        Log?.Debug("Computing the targets for all the unchanged files in the source directory.");
                        var unchangedFiles = OeFile.ConvertToFileToBuild(SourceDirectoryCompletePathList.Where(f => f.State == OeFileState.Unchanged && task.IsPathPassingFilter(f.Path)));
                        taskTarget.SetTargets(unchangedFiles, BaseTargetDirectory);

                        // we add files that should be rebuild by this task because they have new targets in this task.
                        if (rebuildFilesWithNewTargets && PreviouslyBuiltPaths != null) {
                            var nbAdded = baseList.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseTheyHaveNewTargets(unchangedFiles, PreviouslyBuiltPaths));
                            Log?.If(nbAdded > 0)?.Debug($"Added {nbAdded} files to rebuild because they have new targets.");
                        }

                        // we add files that should be rebuild by this task because they have new targets that are missing
                        if (rebuildFilesWithMissingTargets) {
                            var nbAdded = baseList.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseTheyMissingTargets(unchangedFiles));
                            Log?.If(nbAdded > 0)?.Debug($"Added {nbAdded} files to rebuild because they have missing targets.");
                        }
                    }
                }
            }

            Log?.Debug($"A total of {baseList.Count} files are eligible to be built by this task before the filter.");

            return task.FilterFiles(baseList);
        }

        /// <summary>
        /// List all the files of the source directory that need to be rebuild.
        /// </summary>
        private PathList<IOeFile> SourceDirectoryPathListToBuild {
            get {
                if (_sourceDirectoryPathListToBuild == null) {
                    var gitFilterActive = Properties?.BuildOptions?.SourceToBuildGitFilter?.IsActive() ?? false;
                    if (gitFilterActive) {
                        Log?.Debug("Git filter active.");

                        var sourceLister = GetSourceDirectoryFilesLister();
                        sourceLister.GitFilter = Properties?.BuildOptions?.SourceToBuildGitFilter;
                        _sourceDirectoryPathListToBuild = sourceLister.GetFileList();

                        if (PreviouslyBuiltPaths != null) {

                            var nbAdded = _sourceDirectoryPathListToBuild.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(_sourceDirectoryPathListToBuild, PreviouslyBuiltPaths).Where(f => SourceDirectoryCompletePathList.Contains(f)));
                            Log?.If(nbAdded > 0)?.Debug($"Added {nbAdded} files to rebuild because one of their dependencies (i.e. include files) has changed.");
                        }
                    } else {
                        _sourceDirectoryPathListToBuild = SourceDirectoryCompletePathList;

                        if (UseIncrementalBuild && !FullRebuild && PreviouslyBuiltPaths != null) {
                            Log?.Debug("Incremental build active.");

                            // in incremental mode, we are not interested in the files that didn't change, we don't need to rebuild them
                            _sourceDirectoryPathListToBuild = _sourceDirectoryPathListToBuild.CopyWhere(f => f.State != OeFileState.Unchanged);

                            if (Properties != null) {
                                var nbAdded = _sourceDirectoryPathListToBuild.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfTableCrcChanges(Properties.GetEnv(), PreviouslyBuiltPaths).Where(f => SourceDirectoryCompletePathList.Contains(f)));
                                Log?.If(nbAdded > 0)?.Debug($"Added {nbAdded} files to rebuild because one of their referenced table or sequence has changed.");
                            }

                            var nbAddedForDepChange = _sourceDirectoryPathListToBuild.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(_sourceDirectoryPathListToBuild, PreviouslyBuiltPaths).Where(f => SourceDirectoryCompletePathList.Contains(f)));
                            Log?.If(nbAddedForDepChange > 0)?.Debug($"Added {nbAddedForDepChange} files to rebuild because one of their dependencies (i.e. include files) has changed.");

                            if (Properties?.BuildOptions?.IncrementalBuildOptions?.RebuildFilesWithCompilationErrors ?? OeIncrementalBuildOptions.GetDefaultRebuildFilesWithCompilationErrors()) {
                                var nbAddedForNotCompiled = _sourceDirectoryPathListToBuild.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfCompilationErrors(PreviouslyBuiltPaths).Where(f => SourceDirectoryCompletePathList.Contains(f)));
                                Log?.If(nbAddedForNotCompiled > 0)?.Debug($"Added {nbAddedForNotCompiled} files to rebuild because they did not compile correctly in the previous build.");
                            }

                        } else {
                            Log?.Debug("Building every source file.");
                        }
                    }

                    Log?.Debug($"A total of {_sourceDirectoryPathListToBuild.Count} files would need to be (re)built.");
                }
                return _sourceDirectoryPathListToBuild;
            }
        }

        /// <summary>
        /// List all the existing files in the source directory.
        /// </summary>
        internal PathList<IOeFile> SourceDirectoryCompletePathList {
            get {
                if (_sourceDirectoryCompletePathList == null) {
                    var sourceLister = GetSourceDirectoryFilesLister();
                    _sourceDirectoryCompletePathList = sourceLister.GetFileList();
                }
                return _sourceDirectoryCompletePathList;
            }
        }

        /// <summary>
        /// Compiles all the files that need to be compile for all the <see cref="IOeTaskCompile"/> tasks in <see cref="BuildStepExecutor.Tasks"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TaskExecutorException"></exception>
        private IDisposable CompileFiles(out PathList<UoeCompiledFile> compiledPath) {
            var filesToCompile = GetFilesToCompile(Tasks);
            if (filesToCompile.Count == 0) {
                compiledPath = null;
                return null;
            }
            if (Properties == null) {
                throw new TaskExecutorException(this, $"{nameof(Properties)} can't be null.");
            }
            try {
                var compiler = new OeFilesCompiler();
                compiledPath = compiler.CompileFiles(Properties, filesToCompile, CancelToken, Log);
                return compiler;
            } catch (Exception e) {
                throw new TaskExecutorException(this, e.Message, e);
            }
        }

        /// <summary>
        /// Returns a list of all files that need to be compiled for all the <paramref name="tasks" />
        /// </summary>
        /// <remarks>
        /// The main thing that makes this method "complicated" is that we try to find an appropriate UoeFileToCompile.PreferredTargetDirectory.
        /// Considering that the source files are on C:\ and that our output directory is on D:\, we can either :
        /// - compile directly on D:\
        /// - compile in temp dir then move to D:\
        /// but a lot of times, the 1st solution is much faster. This is what we do here...
        /// </remarks>
        /// <param name="tasks"></param>
        /// <returns></returns>
        private PathList<UoeFileToCompile> GetFilesToCompile(IEnumerable<IOeTask> tasks) {

            // list all the tasks that need to compile files.
            var compileTasks = tasks.OfType<IOeTaskCompile>().ToList();

            if (!(Properties?.CompilationOptions?.TryToOptimizeCompilationDirectory ?? OeCompilationOptions.GetDefaultTryToOptimizeCompilationDirectory())) {
                return
                    compileTasks
                    .SelectMany(t => t.GetFilesToProcess())
                    .ToFileList()
                    .CopySelect(f => new UoeFileToCompile(f.Path) { FileSize = f.Size });
            }

            var filesToCompile = compileTasks.SelectMany(t => t.GetFilesToBuild());

            var output = new PathList<UoeFileToCompile>();

            foreach (var groupedBySourcePath in filesToCompile.GroupBy(f => f.Path)) {
                string preferredTargetDirectory = null;

                var allTargets = groupedBySourcePath
                    .Where(file => file.TargetsToBuild != null)
                    .SelectMany(file => file.TargetsToBuild)
                    .Where(target => target is OeTargetFile)
                    .Select(target => target.GetTargetPath()).ToList();

                var firstFileTarget = allTargets.FirstOrDefault();

                if (firstFileTarget != null) {
                    // We found at least one target which is a file.
                    // For this case, no need to compile in a temp folder and then copy it, just compile it directly there.
                    // Except if this file is then copied on different disc drives because in that case, it might not be worth it.
                    if (allTargets.All(s => Utils.ArePathOnSameDrive(s, groupedBySourcePath.Key))) {
                        if (Path.GetFileNameWithoutExtension(firstFileTarget).PathEquals(Path.GetFileNameWithoutExtension(groupedBySourcePath.Key))) {
                            preferredTargetDirectory = Path.GetDirectoryName(firstFileTarget);
                        }
                    }
                }

                output.TryAdd(new UoeFileToCompile(groupedBySourcePath.Key) {
                    FileSize = groupedBySourcePath.First().Size,
                    PreferredTargetDirectory = preferredTargetDirectory
                });
            }

            return output;
        }


        /// <summary>
        /// Gets the file lister for the source directory
        /// </summary>
        /// <returns></returns>
        private PathLister GetSourceDirectoryFilesLister() {
            var sourceLister = new PathLister(SourceDirectory, CancelToken) {
                FilterOptions = Properties?.BuildOptions?.SourceToBuildFilter,
                GitFilter = null,
                Log = Log
            };
            if (UseIncrementalBuild) {
                sourceLister.OutputOptions = new PathListerOutputOptions {
                    GetPreviousFileImage = GetPreviousFileImage,
                    UseCheckSumComparison = Properties?.BuildOptions?.IncrementalBuildOptions?.UseCheckSumComparison ?? OeIncrementalBuildOptions.GetDefaultUseCheckSumComparison(),
                    UseLastWriteDateComparison = true
                };
            }
            return sourceLister;
        }

        private IOeFile GetPreviousFileImage(string filePath) {
            return PreviouslyBuiltPaths?[filePath];
        }
    }
}
