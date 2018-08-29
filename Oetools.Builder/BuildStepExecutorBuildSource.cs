using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder {
    public class BuildStepExecutorBuildSource : BuildStepExecutorWithFileListAndCompilation, IBuildStepExecutorBuildSource {
        
        public FileList<OeFileBuilt> PreviouslyBuiltFiles { get; set; }
        
        public IEnumerable<OeTask> AllTasksOfAllSteps { get; set; }
        
        public bool IsLastBuildStepExecutor { get; set; }

        private bool UseIncrementalBuild => Properties.IncrementalBuildOptions.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled();

        private bool StoreSourceHash => Properties.IncrementalBuildOptions?.StoreSourceHash ?? OeIncrementalBuildOptions.GetDefaultStoreSourceHash();
        
        private bool MirrorDeletedSourceFileToOutput => Properties.IncrementalBuildOptions?.MirrorDeletedSourceFileToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput();
        
        private bool MirrorDeletedTargetsToOutput => Properties.IncrementalBuildOptions?.MirrorDeletedTargetsToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedTargetsToOutput();
        
        private bool RebuildFilesWithNewTargets => Properties.IncrementalBuildOptions?.RebuildFilesWithNewTargets ?? OeIncrementalBuildOptions.GetDefaultRebuildFilesWithNewTargets();

        private string SourceDirectory => Properties.BuildOptions?.SourceDirectoryPath;
        
        private bool FullRebuild => Properties.BuildOptions?.FullRebuild ?? OeBuildOptions.GetDefaultFullRebuild();
        
        public override void Configure() {
            
            var sourceLister = GetSourceDirectoryFilesLister();
            var unfilteredSourceFilesList = sourceLister.GetFileList();
            var completeSourceFilesList = Properties.SourceToBuildGitFilter == null ? unfilteredSourceFilesList : null;

            if (!UseIncrementalBuild || PreviouslyBuiltFiles == null) {
                TaskFiles = unfilteredSourceFilesList;
                return;
            }
            
            // in incremental mode, we are not interested in the files that didn't change, we don't need to rebuild them
            var filesToRebuild = FullRebuild ? unfilteredSourceFilesList : unfilteredSourceFilesList.Where(f => f.State != OeFileState.Unchanged).ToList();

            if (!FullRebuild) {
                var previouslyBuiltCompiled = PreviouslyBuiltFiles.Where(f => f is OeFileBuiltCompiled).Cast<OeFileBuiltCompiled>().ToList();
                
                Log?.Debug("Add files to rebuild because one of the reference table or sequence have changed since the previous build");
                filesToRebuild.AddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfTableCrcChanges(Properties.GetEnv(), previouslyBuiltCompiled));
                
                Log?.Debug("Add files to rebuild because one of their dependencies (think include files) has changed since the previous build");
                filesToRebuild.AddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(filesToRebuild, previouslyBuiltCompiled));
            }

            if (RebuildFilesWithNewTargets || MirrorDeletedTargetsToOutput) {

                if (completeSourceFilesList == null) {
                    Log?.Debug("We used a git filter for the source files, but we now need the complete list of files in the source directory, we do this now");
                    sourceLister.SourcePathGitFilter = null;
                    unfilteredSourceFilesList = sourceLister.GetFileList();
                    completeSourceFilesList = unfilteredSourceFilesList;
                }

                Log?.Debug("Computing all the targets of all the files in the source directory");
                foreach (var task in Tasks) {
                    SetFilesTargets(task, unfilteredSourceFilesList, Properties.BuildOptions.OutputDirectoryPath);
                }

                if (!FullRebuild && RebuildFilesWithNewTargets) {
                    Log?.Info("Adding files with new targets to the build list");
                    filesToRebuild.AddRange(IncrementalBuildHelper.GetSourceFilesToRebuildBecauseTheyHaveNewTargets(unfilteredSourceFilesList, PreviouslyBuiltFiles));
                }
            }
            
            // add files to rebuild
            TaskFiles = new List<OeFile>();
            foreach (var file in sourceLister.FilterSourceFiles(filesToRebuild)) {
                if (!TaskFiles.Exists(f => f.SourceFilePath.Equals(file.SourceFilePath, StringComparison.CurrentCultureIgnoreCase)) && 
                    File.Exists(file.SourceFilePath)) {
                    TaskFiles.Add(file);
                }
            }

            if (IsLastBuildStepExecutor) {
                
                var newTasks = new List<IOeTask>();

                if (MirrorDeletedSourceFileToOutput || MirrorDeletedTargetsToOutput) {
                    Log?.Info("Mirroring deleted files in source to the output directory");
                    Log?.Debug("Making a list of all the files that were deleted since the previous build (the targets of those files must be deleted");
                    var filesToDelete = IncrementalBuildHelper.GetBuiltFilesDeletedSincePreviousBuild(PreviouslyBuiltFiles).ToNonNullList();
                    if (filesToDelete.Any()) {
                        newTasks.Add(new OeTaskTargetsRemover {
                            Label = "Deleting files missing from the previous build",
                            FilesWithTargetsToRemove = filesToDelete
                        });
                    }
                }

                if (MirrorDeletedTargetsToOutput) {
                    Log?.Debug("Computing all the targets for all the tasks of all the files in the source directory");
                    foreach (var task in AllTasksOfAllSteps) {
                        SetFilesTargets(task, completeSourceFilesList, Properties.BuildOptions.OutputDirectoryPath);
                    }

                    Log?.Info("Mirroring deleted targets to the output directory");
                    Log?.Debug("Making a list of all source files that had targets existing in the previous build which don't existing anymore (those targets must be deleted)");
                    var filesToDelete = IncrementalBuildHelper.GetBuiltFilesWithOldTargetsToRemove(completeSourceFilesList, PreviouslyBuiltFiles).ToNonNullList();
                    if (filesToDelete.Any()) {
                        newTasks.Add(new OeTaskTargetsRemover {
                            Label = "Deleting previous targets that no longer exist", 
                            FilesWithTargetsToRemove = filesToDelete
                        });
                    }
                }

                // add extra tasks to remove targets
                Tasks.AddRange(newTasks.Where(t => t != null));
            }

        }
        
        /// <summary>
        /// Gets the file lister for the source directory
        /// </summary>
        /// <returns></returns>
        private SourceFilesLister GetSourceDirectoryFilesLister() {
            var sourceLister = new SourceFilesLister(SourceDirectory, CancelSource) {
                SourcePathFilter = Properties.SourceToBuildPathFilter,
                SourcePathGitFilter = Properties.SourceToBuildGitFilter,
                Log = Log
            };
            if (UseIncrementalBuild) {
                sourceLister.SetFileInfoAndState = true;
                sourceLister.PreviousSourceFiles = PreviouslyBuiltFiles;
                sourceLister.UseHashComparison = StoreSourceHash;
                sourceLister.UseLastWriteDateComparison = true;
            }
            return sourceLister;
        }
    }
}