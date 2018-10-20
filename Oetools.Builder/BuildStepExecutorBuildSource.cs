using System;
using System.Collections.Generic;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
    
    public class BuildStepExecutorBuildSource : BuildStepExecutor {
        
        protected override string BaseTargetDirectory => Properties.BuildOptions?.OutputDirectoryPath;
        
        public PathList<OeFileBuilt> PreviouslyBuiltPaths { private get; set; }

        private bool UseIncrementalBuild => Properties.IncrementalBuildOptions?.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled();
        
        private string SourceDirectory => Properties.BuildOptions?.SourceDirectoryPath;
        
        private bool FullRebuild => Properties.IncrementalBuildOptions?.FullRebuild ?? OeIncrementalBuildOptions.GetDefaultFullRebuild();
        
        private PathList<UoeCompiledFile> _compiledPaths;
        
        private PathList<OeFile> _sourceDirectoryPathListToBuild;

        /// <inheritdoc cref="BuildStepExecutor.ExecuteInternal"/>
        protected override void ExecuteInternal() {
            if (!TestMode) {
                Log?.Info("Compiling files from all tasks before executing all the tasks");
                CompileFiles();
                if (_compiledPaths != null) {
                    Log?.Debug("Associate the list of compiled files for each task");
                    foreach (var task in Tasks) {
                        if (task is IOeTaskCompile taskCompile) {
                            taskCompile.SetCompiledFiles(_compiledPaths?.CopyWhere(cf => taskCompile.GetFilesToBuild().Contains(cf.Path)));
                        }
                    }
                }
            }
            base.ExecuteInternal();
        }

        /// <inheritdoc cref="BuildStepExecutor.GetFilesToBuildForSingleTask"/>
        protected override PathList<OeFile> GetFilesToBuildForSingleTask(IOeTaskFile task) {
            var baseList = SourceDirectoryPathListToBuild;
            
            if (task is IOeTaskFileTarget taskTarget) {
                // we add files that should be rebuild by this task because they have new targets in this task.
                var rebuildFilesWithNewTargets = Properties.IncrementalBuildOptions?.RebuildFilesWithNewTargets ?? OeIncrementalBuildOptions.GetDefaultRebuildFilesWithNewTargets();
                if (rebuildFilesWithNewTargets && !FullRebuild && UseIncrementalBuild && PreviouslyBuiltPaths != null) {

                    Log?.Debug("Need to compute the targets for all the unchanged files in the source directory");
                    var unchangedFiles = SourceDirectoryCompletePathList.CopyWhere(f => f.State == OeFileState.Unchanged);
                    taskTarget.SetTargetForFiles(unchangedFiles, BaseTargetDirectory);

                    Log?.Info("Adding files with new targets to the build list");
                    baseList.TryAddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseTheyHaveNewTargets(unchangedFiles, PreviouslyBuiltPaths));
                }
            }

            return task.FilterFiles(baseList);
        }
        
        /// <summary>
        /// List all the files of the source directory that need to be rebuild.
        /// </summary>
        private PathList<OeFile> SourceDirectoryPathListToBuild {
            get {
                if (_sourceDirectoryPathListToBuild == null) {
                    var sourceLister = GetSourceDirectoryFilesLister();
                    _sourceDirectoryPathListToBuild = sourceLister.GetFileList();
                    _sourceDirectoryCompletePathList = Properties.SourceToBuildGitFilterOptions?.IsActive() ?? false ? _sourceDirectoryPathListToBuild : null;
                    
                    if (Properties.SourceToBuildGitFilterOptions?.IsActive() ?? false) {

                        Log?.Debug("Git filter active");
                        
                        if (PreviouslyBuiltPaths != null) {
                            
                            Log?.Debug("Add files to rebuild because one of their dependencies (think include files) has changed");
                            _sourceDirectoryPathListToBuild.AddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(_sourceDirectoryPathListToBuild, PreviouslyBuiltPaths.OfType<OeFileBuiltCompiled>().ToList()));
                        }

                    } else if (!FullRebuild && UseIncrementalBuild && PreviouslyBuiltPaths != null) {

                        Log?.Debug("Incremental build active");

                        // in incremental mode, we are not interested in the files that didn't change, we don't need to rebuild them
                        _sourceDirectoryPathListToBuild = _sourceDirectoryPathListToBuild.CopyWhere(f => f.State != OeFileState.Unchanged);

                        var previouslyBuiltCompiled = PreviouslyBuiltPaths.OfType<OeFileBuiltCompiled>().ToList();

                        Log?.Debug("Add files to rebuild because one of the reference table or sequence have changed since the previous build");
                        _sourceDirectoryPathListToBuild.AddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfTableCrcChanges(Properties.GetEnv(), previouslyBuiltCompiled));

                        Log?.Debug("Add files to rebuild because one of their dependencies (think include files) has changed since the previous build");
                        _sourceDirectoryPathListToBuild.AddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(_sourceDirectoryPathListToBuild, previouslyBuiltCompiled));

                    } else {

                        Log?.Debug("Build every source file");
                        
                        return _sourceDirectoryPathListToBuild;

                    }
                }
                return _sourceDirectoryPathListToBuild;
            }
        }
        
        private PathList<OeFile> _sourceDirectoryCompletePathList;
        
        /// <summary>
        /// List all the existing files in the source directory.
        /// </summary>
        public PathList<OeFile> SourceDirectoryCompletePathList {
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
        private void CompileFiles() {
            var filesToCompile = GetFilesToCompile(Tasks);
            if (filesToCompile.Count == 0) {
                return;
            }
            if (Properties == null) {
                throw new ArgumentNullException(nameof(Properties));
            }
            try {
                _compiledPaths = OeFilesCompiler.CompileFiles(Properties, filesToCompile, CancelToken, Log);
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
            
            //if (!(Properties?.CompilationOptions?.TryToOptimizeCompilationDirectory ?? OeCompilationOptions.GetDefaultTryToOptimizeCompilationDirectory())) {
                return 
                    compileTasks
                    .SelectMany(t => t.GetFilesToBuild())
                    .ToFileList()
                    .CopySelect(f => new UoeFileToCompile(f.Path) { FileSize = f.Size });
            //}
            /*
            var filesToCompile = new FileList<UoeFileToCompile>();
            
            foreach (var file in files) {
                
                // get all the compile tasks that handle this file
                var compileTasksForThisFile = compileTasks.Where(t => t.IsFilePassingFilter(file.FilePath)).ToList();
                if (compileTasksForThisFile.Count == 0) {
                    continue;
                }

                // set all the targets (from all compile tasks) for this file, we need this to set UoeFileToCompile.PreferedTargetDirectory
                foreach (var task in compileTasksForThisFile) {
                    if (task is IOeTaskFileTargetFile taskWithTargetFiles) {
                        (file.TargetsFiles ?? (file.TargetsFiles = new List<OeTargetFile>())).AddRange(taskWithTargetFiles.GetTargetsFiles(file.FilePath, baseTargetDirectory));
                    }

                    if (task is IOeTaskFileTargetArchive taskWithTargetArchives) {
                        (file.TargetsArchives ?? (file.TargetsArchives = new List<OeTargetArchive>())).AddRange(taskWithTargetArchives.GetTargetsArchives(file.FilePath, baseTargetDirectory));
                    }
                }

                string preferedTargetDirectory = null;
                var allTargets = file.TargetsArchives?.Select(a => a.TargetPackFilePath).UnionHandleNull(file.TargetsFiles?.Select(f => f.TargetFilePath));
                var firstTargetWithDifferentDiscDrive = allTargets?.FirstOrDefault(s => !Utils.ArePathOnSameDrive(s, file.FilePath));
                if (firstTargetWithDifferentDiscDrive != null) {
                    // basically, we found 1 file that targets a different disc drive
                    if (allTargets.All(s => Utils.ArePathOnSameDrive(s, firstTargetWithDifferentDiscDrive))) {
                        // and all targets are on the same disc drive, it is worth targetting this
                        // TODO : maybe the first won't do, search for one that does
                        if (Path.GetFileNameWithoutExtension(firstTargetWithDifferentDiscDrive).EqualsCi(Path.GetFileNameWithoutExtension(file.FilePath))) {
                            preferedTargetDirectory = Path.GetDirectoryName(firstTargetWithDifferentDiscDrive);
                        }
                    }
                }
                
                // at least one compile task takes care of this file, we need to compile it
                filesToCompile.TryAdd(new UoeFileToCompile(file.FilePath) {
                    FileSize = file.Size,
                    PreferedTargetDirectory = preferedTargetDirectory
                });
            }

            return filesToCompile;
            */
        }
        
        /// <summary>
        /// Gets the file lister for the source directory
        /// </summary>
        /// <returns></returns>
        private PathLister GetSourceDirectoryFilesLister() {
            var sourceLister = new PathLister(SourceDirectory, CancelToken) {
                FilterOptions = Properties.SourceToBuildFilter,
                GitFilter = Properties.SourceToBuildGitFilterOptions,
                Log = Log
            };
            if (UseIncrementalBuild) {
                sourceLister.OutputOptions = new PathListerOutputOptions {
                    GetPreviousFileImage = GetPreviousFileImage,
                    UseHashComparison = Properties.IncrementalBuildOptions?.StoreSourceHash ?? OeIncrementalBuildOptions.GetDefaultStoreSourceHash(),
                    UseLastWriteDateComparison = true
                };
            }
            return sourceLister;
        }

        private OeFile GetPreviousFileImage(string filePath) {
            return PreviouslyBuiltPaths?[filePath];
        }
    }
}