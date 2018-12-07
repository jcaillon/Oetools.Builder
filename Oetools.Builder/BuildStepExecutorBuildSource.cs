using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    
    public class BuildStepExecutorBuildSource : BuildStepExecutor {
        
        protected override string BaseTargetDirectory => Properties?.BuildOptions?.OutputDirectoryPath;
        
        public PathList<IOeFileBuilt> PreviouslyBuiltPaths { private get; set; }

        public bool IsLastSourceExecutor { get; set; }

        public bool IsFirstSourceExecutor { get; set; }

        public List<OeBuildStepBuildSource> StepsList { get; set; }

        private bool UseIncrementalBuild => Properties?.BuildOptions?.IncrementalBuildOptions?.EnabledIncrementalBuild ?? OeIncrementalBuildOptions.GetDefaultEnabledIncrementalBuild();
        
        private string SourceDirectory => (Properties?.BuildOptions?.SourceDirectoryPath).TakeDefaultIfNeeded(OeBuildOptions.GetDefaultSourceDirectoryPath());
        
        private bool FullRebuild => Properties?.BuildOptions?.FullRebuild ?? OeBuildOptions.GetDefaultFullRebuild();
        
        private PathList<UoeCompiledFile> _compiledPaths;
        
        private PathList<IOeFile> _sourceDirectoryPathListToBuild;
        
        private PathList<IOeFile> _sourceDirectoryCompletePathList;

        /// <inheritdoc cref="BuildStepExecutor.ExecuteInternal"/>
        protected override void ExecuteInternal(List<IOeTask> tasks) {
            // remove delete files targets
            if (IsFirstSourceExecutor) {
                Log?.Debug("Is the first build source step.");
                var removalTask = GetRemovalTasksForDeletedFiles();
                if (removalTask != null) {
                    Log?.Debug("New removal task for previous files not existing anymore.");
                    InjectPropertiesInTask(removalTask);
                    Tasks?.Insert(0, removalTask);
                }
            }
            
            IDisposable compiler = null;
            if (!TestMode) {
                Log?.Debug("Compiling files from all tasks.");
                compiler = CompileFiles();
                if (_compiledPaths != null) {
                    Log?.Debug("Associate the list of compiled files for each task.");
                    foreach (var task in Tasks) {
                        if (task is IOeTaskCompile taskCompile) {
                            taskCompile.SetCompiledFiles(_compiledPaths?.CopyWhere(cf => taskCompile.GetFilesToProcess().Contains(cf.Path)));
                        }
                    }
                }
            }
            try {
                base.ExecuteInternal(tasks);
            } finally {
                compiler?.Dispose();
            }
            
            // add + execute removal tasks?
            if (IsLastSourceExecutor) {
                Log?.Debug("Is the last build source step.");
                var removalTask = GetRemovalTasksForDeletedTargets();
                if (removalTask != null) {
                    Log?.Debug("New removal task for previous targets not existing anymore.");
                    InjectPropertiesInTask(removalTask);
                    Tasks?.Add(removalTask);
                    base.ExecuteInternal(new List<IOeTask> { removalTask });
                }
            }
        }

        /// <inheritdoc cref="BuildStepExecutor.GetFilesToBuildForSingleTask"/>
        protected override PathList<IOeFile> GetFilesToBuildForSingleTask(IOeTaskFile task) {
            Log?.Debug($"Getting the list of files to build for task: {task.ToString().PrettyQuote()}.");
            
            var baseList = SourceDirectoryPathListToBuild;
            
            if (task is IOeTaskFileToBuild taskTarget) {
                // we add files that should be rebuild by this task because they have new targets in this task.
                var rebuildFilesWithNewTargets = Properties?.BuildOptions?.IncrementalBuildOptions?.RebuildFilesWithNewTargets ?? OeIncrementalBuildOptions.GetDefaultRebuildFilesWithNewTargets();
                if (rebuildFilesWithNewTargets && !FullRebuild && UseIncrementalBuild && PreviouslyBuiltPaths != null) {

                    Log?.Debug("Computing the targets for all the unchanged files in the source directory.");
                    var unchangedFiles = OeFile.ConvertToFileToBuild(SourceDirectoryCompletePathList.Where(f => f.State == OeFileState.Unchanged && task.IsPathPassingFilter(f.Path)));
                    taskTarget.SetTargets(unchangedFiles, BaseTargetDirectory);

                    var nbAdded = baseList.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseTheyHaveNewTargets(unchangedFiles, PreviouslyBuiltPaths));
                    Log?.If(nbAdded > 0)?.Debug($"Added {nbAdded} files to rebuild because they have new targets.");
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
                    var sourceLister = GetSourceDirectoryFilesLister();
                    _sourceDirectoryPathListToBuild = sourceLister.GetFileList();
                    _sourceDirectoryCompletePathList = Properties?.BuildOptions?.SourceToBuildGitFilter?.IsActive() ?? false ? _sourceDirectoryPathListToBuild : null;
                    
                    if (Properties?.BuildOptions?.SourceToBuildGitFilter?.IsActive() ?? false) {

                        Log?.Debug("Git filter active.");
                        
                        if (PreviouslyBuiltPaths != null) {
                            
                            var nbAdded = _sourceDirectoryPathListToBuild.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(_sourceDirectoryPathListToBuild, PreviouslyBuiltPaths));
                            Log?.If(nbAdded > 0)?.Debug($"Added {nbAdded} files to rebuild because one of their dependencies (i.e. include files) has changed.");
                        }

                    } else if (!FullRebuild && UseIncrementalBuild && PreviouslyBuiltPaths != null) {

                        Log?.Debug("Incremental build active.");

                        // in incremental mode, we are not interested in the files that didn't change, we don't need to rebuild them
                        _sourceDirectoryPathListToBuild = _sourceDirectoryPathListToBuild.CopyWhere(f => f.State != OeFileState.Unchanged);

                        if (Properties != null) {
                            var nbAdded = _sourceDirectoryPathListToBuild.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfTableCrcChanges(Properties.GetEnv(), PreviouslyBuiltPaths));
                            Log?.If(nbAdded > 0)?.Debug($"Added {nbAdded} files to rebuild because one of their referenced table or sequence has changed.");
                        }

                        var nbAddedForDepChange = _sourceDirectoryPathListToBuild.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(_sourceDirectoryPathListToBuild, PreviouslyBuiltPaths));
                        Log?.If(nbAddedForDepChange > 0)?.Debug($"Added {nbAddedForDepChange} files to rebuild because one of their dependencies (i.e. include files) has changed.");

                        if (Properties?.BuildOptions?.IncrementalBuildOptions?.RebuildFilesWithCompilationErrors ?? OeIncrementalBuildOptions.GetDefaultRebuildFilesWithCompilationErrors()) {
                            var nbAddedForNotCompiled = _sourceDirectoryPathListToBuild.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfCompilationErrors(PreviouslyBuiltPaths));
                            Log?.If(nbAddedForNotCompiled > 0)?.Debug($"Added {nbAddedForNotCompiled} files to rebuild because they did not compile correctly in the previous build.");
                        }

                    } else {
                        Log?.Debug("Building every source file.");
                    }
                    
                    Log?.Debug($"A total of {_sourceDirectoryPathListToBuild.Count} files would need to be (re)built.");
                }
                return _sourceDirectoryPathListToBuild;
            }
        }
        
        /// <summary>
        /// List all the existing files in the source directory.
        /// </summary>
        public PathList<IOeFile> SourceDirectoryCompletePathList {
            get {
                if (SourceDirectoryPathListToBuild != null && _sourceDirectoryCompletePathList == null) {
                    var sourceLister = GetSourceDirectoryFilesLister();
                    sourceLister.GitFilter = null;
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
        private IDisposable CompileFiles() {
            var filesToCompile = GetFilesToCompile(Tasks);
            if (filesToCompile.Count == 0) {
                return null;
            }
            if (Properties == null) {
                throw new ArgumentNullException(nameof(Properties));
            }
            try {
                var compiler = new OeFilesCompiler();
                _compiledPaths = compiler.CompileFiles(Properties, filesToCompile, CancelToken, Log);
                return compiler;
            } catch (Exception e) {
                throw new TaskExecutorException(this, e.Message, e);
            }
        }

        /// <summary>
        /// Returns a list of all files that need to be compiled for all the <paramref name="tasks" />
        /// </summary>
        /// <remarks>
        /// The main thing that makes this method "complicated" is that we try to an appropriate UoeFileToCompile.PreferedTargetDirectory
        /// consider that the source files are on C:\ and that our output directory is on D:\, we can either :
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
        /// Allow to delete obsolete files/targets there were previously built.
        /// </summary>
        private IOeTask GetRemovalTasksForDeletedTargets() {
            if (PreviouslyBuiltPaths == null) {
                return null;
            }
            
            if (Properties?.BuildOptions?.IncrementalBuildOptions?.MirrorDeletedTargetsToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedTargetsToOutput()) {
                Log?.Debug("Mirroring deleted targets to the output directory.");
                
                var unchangedOrModifiedFiles = OeFile.ConvertToFileToBuild(SourceDirectoryCompletePathList?.Where(f => f.State == OeFileState.Unchanged || f.State == OeFileState.Modified));

                if (unchangedOrModifiedFiles != null && unchangedOrModifiedFiles.Count > 0) {
                    Log?.Debug("For all the unchanged or modified files, set all the targets that should be built in this build");
                    foreach (var task in StepsList.SelectMany(step1 => (step1.Tasks?.OfType<IOeTaskFileToBuild>()).ToNonNullEnumerable())) {
                        task.SetTargets(unchangedOrModifiedFiles.CopyWhere(f => task.IsPathPassingFilter(f.Path)), Properties?.BuildOptions?.OutputDirectoryPath, true);
                    }

                    Log?.Debug("Making a list of all source files that had targets existing in the previous build which don't existing anymore (those targets must be deleted)");
                    var filesWithTargetsToRemove = IncrementalBuildHelper.GetBuiltFilesWithOldTargetsToRemove(unchangedOrModifiedFiles, PreviouslyBuiltPaths).ToFileList();

                    if (filesWithTargetsToRemove != null && filesWithTargetsToRemove.Count > 0) {
                        var newTask = new OeTaskTargetsDeleter {
                            Name = "Deleting previous targets that no longer exist"
                        };
                        newTask.SetFilesWithTargetsToRemove(filesWithTargetsToRemove);
                        return newTask;
                    }
                }
                Log?.Debug("There is nothing to remove.");
            }

            return null;
        }
        
        /// <summary>
        /// Allow to delete obsolete files/targets there were previously built.
        /// </summary>
        private IOeTask GetRemovalTasksForDeletedFiles() {
            if (PreviouslyBuiltPaths == null) {
                return null;
            }

            var mirrorDeletedSourceFileToOutput = Properties?.BuildOptions?.IncrementalBuildOptions?.MirrorDeletedSourceFileToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput();
            var mirrorDeletedTargetsToOutput = Properties?.BuildOptions?.IncrementalBuildOptions?.MirrorDeletedTargetsToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedTargetsToOutput();

            if (mirrorDeletedSourceFileToOutput || mirrorDeletedTargetsToOutput) {
                Log?.Debug("Mirroring deleted files in source to the output directory.");

                Log?.Debug("Making a list of all the files that were deleted since the previous build (the targets of those files must be deleted.");
                var filesWithTargetsToRemove = IncrementalBuildHelper.GetBuiltFilesDeletedSincePreviousBuild(SourceDirectoryCompletePathList, PreviouslyBuiltPaths).ToFileList();
                if (filesWithTargetsToRemove != null && filesWithTargetsToRemove.Count > 0) {
                    var newTask = new OeTaskTargetsDeleter {
                        Name = "Deleting files missing from the previous build"
                    };
                    newTask.SetFilesWithTargetsToRemove(filesWithTargetsToRemove);
                    return newTask;
                }
                Log?.Debug("There is nothing to remove.");
            }

            return null;
        }
        
        /// <summary>
        /// Gets the file lister for the source directory
        /// </summary>
        /// <returns></returns>
        private PathLister GetSourceDirectoryFilesLister() {
            var sourceLister = new PathLister(SourceDirectory, CancelToken) {
                FilterOptions = Properties?.BuildOptions?.SourceToBuildFilter,
                GitFilter = Properties?.BuildOptions?.SourceToBuildGitFilter,
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