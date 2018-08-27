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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

[assembly: InternalsVisibleTo("Oetools.Builder.Test")]

namespace Oetools.Builder {
    
    public class Builder : IDisposable {

        public ILogger Log { protected get; set; }
      
        public OeBuildConfiguration BuildConfiguration { get; }

        public OeBuildHistory BuildHistory { get; private set; }

        public List<OeFileBuilt> PreviouslyBuiltFiles { get; set; }

        public List<BuildStepExecutor> BuildStepExecutors { get; } = new List<BuildStepExecutor>();
        
        public CancellationTokenSource CancelSource { get; } = new CancellationTokenSource();

        private string BuildTemporaryDirectory { get; set; }

        protected bool UseIncrementalBuild => BuildConfiguration.Properties.IncrementalBuildOptions.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled();

        private bool StoreSourceHash => BuildConfiguration.Properties.IncrementalBuildOptions?.StoreSourceHash ?? OeIncrementalBuildOptions.GetDefaultStoreSourceHash();
        
        private bool MirrorDeletedSourceFileToOutput => BuildConfiguration.Properties.IncrementalBuildOptions?.MirrorDeletedSourceFileToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput();
        
        private bool MirrorDeletedTargetsToOutput => BuildConfiguration.Properties.IncrementalBuildOptions?.MirrorDeletedTargetsToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedTargetsToOutput();
        
        private bool RebuildFilesWithNewTargets => BuildConfiguration.Properties.IncrementalBuildOptions?.RebuildFilesWithNewTargets ?? OeIncrementalBuildOptions.GetDefaultRebuildFilesWithNewTargets();

        protected string SourceDirectory => BuildConfiguration.Properties.BuildOptions?.SourceDirectoryPath;
        
        public bool FullRebuild => BuildConfiguration.Properties.BuildOptions?.FullRebuild ?? OeBuildOptions.GetDefaultFullRebuild();
        
        public List<TaskExecutionException> TaskExecutionExceptions => BuildStepExecutors
            .SelectMany(exec => (exec?.Tasks).ToNonNullList())
            .SelectMany(task => task.GetExceptionList().ToNonNullList())
            .ToList();
        
        /// <summary>
        /// Initiliaze the build
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
                BuildHistory = GetBuildHistory();
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
            ExecuteBuildStep<BuildStepExecutor>(BuildConfiguration.PreBuildStepGroup, nameof(OeBuildConfiguration.PreBuildStepGroup), null);
            ExecuteBuildStep<BuildStepExecutorWithFileListAndCompilation>(BuildConfiguration.BuildSourceStepGroup, nameof(OeBuildConfiguration.BuildSourceStepGroup), TaskExecutorConfiguratorBuildSource);
            ExecuteBuildStep<BuildStepExecutorWithFileList>(BuildConfiguration.BuildOutputStepGroup, nameof(OeBuildConfiguration.BuildOutputStepGroup), TaskExecutorConfiguratorBuildOutput);
            ExecuteBuildStep<BuildStepExecutor>(BuildConfiguration.PostBuildStepGroup, nameof(OeBuildConfiguration.PostBuildStepGroup), null);
        }

        /// <summary>
        /// Executes a build step
        /// </summary>
        private void ExecuteBuildStep<T>(IEnumerable<OeBuildStep> steps, string oeBuildConfigurationPropertyName, Action<T> taskExecutorConfigurator) where T : BuildStepExecutor, new() {
            var executionName = typeof(OeBuildConfiguration).GetXmlName(oeBuildConfigurationPropertyName);
            
            Log?.Debug($"Starting {executionName}");
            
            if (steps != null) {
                var i = 0;
                foreach (var step in steps) {
                    Log?.Debug($"Starting {executionName} - {step}");
                    var executor = new T {
                        Name = executionName,
                        Id = i,
                        Tasks = (step.GetTaskList()?.Cast<IOeTask>()).ToNonNullList(),
                        Properties = BuildConfiguration.Properties,
                        Log = Log,
                        CancelSource = CancelSource
                    };
                    taskExecutorConfigurator?.Invoke(executor);
                    BuildStepExecutors.Add(executor);
                    executor.Execute();
                    i++;
                }
            }
        }

        private void TaskExecutorConfiguratorBuildOutput(BuildStepExecutorWithFileList executor) {
            if (Directory.Exists(BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath)) {
                var sourceLister = new SourceFilesLister(BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, CancelSource) {
                    Log = Log
                };
                executor.TaskFiles = sourceLister.GetFileList();
            }
        }

        private void TaskExecutorConfiguratorBuildSource(BuildStepExecutorWithFileListAndCompilation executor) {
            var sourceLister = GetSourceDirectoryFilesLister();
            var unfilteredSourceFilesList = sourceLister.GetFileList();
            
            if (UseIncrementalBuild && PreviouslyBuiltFiles != null) {

                var newTasks = new List<IOeTask>();
                
                // in incremental mode, we are not interested in the files that didn't change, we don't need to rebuild them
                var filesToRebuild = FullRebuild ? unfilteredSourceFilesList : unfilteredSourceFilesList.Where(f => f.State != OeFileState.Unchanged).ToList();

                if (!FullRebuild) {
                    var previouslyBuiltCompiled = PreviouslyBuiltFiles.Where(f => f is OeFileBuiltCompiled).Cast<OeFileBuiltCompiled>().ToList();
                    
                    Log?.Debug("Add files to rebuild because one of the reference table or sequence have changed since the previous build");
                    filesToRebuild.AddRange(GetSourceFilesToRebuildBecauseOfTableCrcChanges(BuildConfiguration.Properties.GetEnv(), previouslyBuiltCompiled));
                    
                    Log?.Debug("Add files to rebuild because one of their dependencies (think include files) has changed since the previous build");
                    filesToRebuild.AddRange(GetSourceFilesToRebuildBecauseOfDependencesModification(filesToRebuild, previouslyBuiltCompiled));
                }

                if (MirrorDeletedSourceFileToOutput || MirrorDeletedTargetsToOutput) {
                    Log?.Info("Mirroring deleted files in source to the output directory");
                    newTasks.Add(GetTaskSourceRemover(PreviouslyBuiltFiles, Log));
                }

                if (RebuildFilesWithNewTargets || MirrorDeletedTargetsToOutput) {

                    if (BuildConfiguration.Properties.SourceToBuildGitFilter != null) {
                        Log?.Debug("We used a git filter for the source files, but we now need the complete list of files in the source directory, we do this now");
                        sourceLister.SourcePathGitFilter = null;
                        unfilteredSourceFilesList = sourceLister.GetFileList();
                    }

                    Log?.Debug("Computing all the targets of all the files in the source directory");
                    foreach (var task in executor.Tasks) {
                        BuildStepExecutor.SetFilesTargets(task, unfilteredSourceFilesList, BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath);
                    }

                    if (!FullRebuild && RebuildFilesWithNewTargets) {
                        Log?.Info("Adding files with new targets to the build list");
                        filesToRebuild.AddRange(GetSourceFilesToRebuildBecauseTheyHaveNewTargets(unfilteredSourceFilesList, PreviouslyBuiltFiles));
                    }

                    if (MirrorDeletedTargetsToOutput) {
                        Log?.Info("Mirroring deleted targets to the output directory");
                        newTasks.Add(GetTaskTargetsRemover(unfilteredSourceFilesList, PreviouslyBuiltFiles, Log));
                    }
                }
                
                // add files to rebuild
                foreach (var file in sourceLister.FilterSourceFiles(filesToRebuild)) {
                    if (!executor.TaskFiles.Exists(f => f.SourceFilePath.Equals(file.SourceFilePath, StringComparison.CurrentCultureIgnoreCase)) && 
                        File.Exists(file.SourceFilePath)) {
                        executor.TaskFiles.Add(file);
                    }
                }
                
                // add extra tasks to remove targets
                executor.Tasks.AddRange(newTasks.Where(t => t != null));
                
            } else {
                
                executor.TaskFiles = unfilteredSourceFilesList;
            }
        }
        
        private OeBuildHistory GetBuildHistory() {
            var history = new OeBuildHistory {
                BuiltFiles = GetFilesBuiltHistory(),
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
        private List<OeFileBuilt> GetFilesBuiltHistory() {
            
            var builtFiles = new Dictionary<string, OeFileBuilt>(StringComparer.CurrentCultureIgnoreCase);
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
                
                if (!builtFiles.ContainsKey(fileBuilt.SourceFilePath)) {
                    builtFiles.Add(fileBuilt.SourceFilePath, (OeFileBuilt) Utils.DeepCopyPublicProperties(fileBuilt, fileBuilt.GetType()));
                }
                OeFileBuilt historyFileBuilt = builtFiles[fileBuilt.SourceFilePath];
                if (historyFileBuilt.Targets == null) {
                    historyFileBuilt.Targets = targetsOutputDirectory;
                } else {
                    historyFileBuilt.Targets.AddRange(targetsOutputDirectory);
                }
            }
            
            // add all the files that were not rebuild from the previous build history
            if (PreviouslyBuiltFiles != null) {
                foreach (var previousFile in PreviouslyBuiltFiles.Where(oldFile => oldFile.State != OeFileState.Deleted && !builtFiles.ContainsKey(oldFile.SourceFilePath))) {
                    builtFiles.Add(previousFile.SourceFilePath, previousFile);
                }
            }

            var output = builtFiles.Values.ToList();
            
            // also ensures that all files have HASH info
            if (UseIncrementalBuild && StoreSourceHash) {
                output.ForEach(f => {
                    if (f.State != OeFileState.Deleted) {
                        SourceFilesLister.SetFileHash(f);
                    }
                });
            }

            return output;
        }
        
        /// <summary>
        /// Gets the file lister for the source directory
        /// </summary>
        /// <returns></returns>
        private SourceFilesLister GetSourceDirectoryFilesLister() {
            var sourceLister = new SourceFilesLister(SourceDirectory, CancelSource) {
                SourcePathFilter = BuildConfiguration.Properties.SourceToBuildPathFilter,
                SourcePathGitFilter = BuildConfiguration.Properties.SourceToBuildGitFilter,
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
        
        internal static IEnumerable<OeFile> GetSourceFilesToRebuildBecauseTheyHaveNewTargets(List<OeFile> allExistingSourceFilesWithSetTargets, List<OeFileBuilt> previousFilesBuilt) {
            foreach (var newFile in allExistingSourceFilesWithSetTargets.Where(file => file.State == OeFileState.Unchanged)) {
                var previousFile = previousFilesBuilt.First(prevFile => prevFile.SourceFilePath.Equals(newFile.SourceFilePath, StringComparison.CurrentCultureIgnoreCase));
                var previouslyCreatedTargets = previousFile.Targets.ToNonNullList().Where(target => !target.IsDeletionMode()).Select(t => t.GetTargetPath()).ToList();
                foreach (var targetPath in newFile.GetAllTargets().Select(t => t.GetTargetPath())) {
                    if (!previouslyCreatedTargets.Exists(prevTarget => prevTarget.Equals(targetPath, StringComparison.CurrentCultureIgnoreCase))) {
                        yield return newFile;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a raw list of files that need to be rebuilt because one of their dependencies (source file, include) has been modified (modified/deleted)
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list
        /// </summary>
        /// <param name="filesModified"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<OeFile> GetSourceFilesToRebuildBecauseOfDependencesModification(List<OeFile> filesModified, List<OeFileBuiltCompiled> previousFilesBuilt) {
            for (int i = 0; i < filesModified.Count; i++) {
                bool firstAdd = true;
                foreach (var result in previousFilesBuilt.Where(prevf => prevf.RequiredFiles != null && 
                    prevf.RequiredFiles.Any(prevFile => filesModified[i].SourceFilePath.Equals(prevFile, StringComparison.CurrentCultureIgnoreCase)))) {
                    if (firstAdd) {
                        filesModified.Add(result);
                    }
                    firstAdd = false;
                    yield return result.GetDeepCopy();
                }
            }
        }
        
        /// <summary>
        /// Returns a raw list of files that need to be rebuilt because one of their database references has been modified (modified/deleted)
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list
        /// </summary>
        /// <param name="env"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<OeFile> GetSourceFilesToRebuildBecauseOfTableCrcChanges(UoeExecutionEnv env, List<OeFileBuiltCompiled> previousFilesBuilt) {
            var sequences = env.Sequences;
            var tables = env.TablesCrc;
            
            // add all previous that required a database reference that has now changed
            foreach (var previousFile in previousFilesBuilt) {
                var allReferencesOk = previousFile.RequiredDatabaseReferences?.All(dRef => {
                    switch (dRef) {
                        case OeDatabaseReferenceSequence sequence:
                            return sequences.Contains(sequence.QualifiedName);
                        case OeDatabaseReferenceTable table:
                            return tables.ContainsKey(table.QualifiedName) && tables[table.QualifiedName].EqualsCi(table.Crc);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }) ?? true;
                if (!allReferencesOk) {
                    yield return previousFile.GetDeepCopy();
                }
            }
        }
        
        internal static IOeTask GetTaskSourceRemover(List<OeFileBuilt> previousFilesBuilt, ILogger log) {
            log?.Debug("Making a list of all the files that were deleted since the previous build (the targets of those files must be deleted");
            var deletedFileList = new List<OeFileBuilt>();
            foreach (var previousSourceFile in previousFilesBuilt.Where(f => f.State != OeFileState.Deleted)) {
                if (!File.Exists(previousSourceFile.SourceFilePath)) {
                    if (previousSourceFile.Targets != null && previousSourceFile.Targets.Count > 0) {
                        previousSourceFile.Targets.ForEach(target => target.SetDeletionMode());
                        deletedFileList.Add(previousSourceFile);
                    }
                }
            }
            if (deletedFileList.Count > 0) {
                log?.Debug($"Added {deletedFileList.Count} files to the {typeof(OeTaskTargetsRemover).GetXmlName()} task because they no longer exist in the source directory, their targets will be deleted");
                return new OeTaskTargetsRemover {
                    Label = "Deleting files missing from the previous build",
                    FilesWithTargetsToRemove = deletedFileList
                };
            }
            
            return null;
        }

        internal static IOeTask GetTaskTargetsRemover(List<OeFile> allExistingSourceFilesWithSetTargets, List<OeFileBuilt> previousFilesBuilt, ILogger log) {
            log?.Debug("Making a list of all unchanged files that have targets existing in the previous build but not existing anymore (those targets must be deleted)");
            
            var filesWithTargetsToDelete = new List<OeFileBuilt>();
            foreach (var previousFile in previousFilesBuilt.Where(file => file.State != OeFileState.Deleted)) {
                var newFile = allExistingSourceFilesWithSetTargets.FirstOrDefault(nFile => nFile.SourceFilePath.Equals(previousFile.SourceFilePath, StringComparison.CurrentCultureIgnoreCase));
                // if the newFile doesn't exist, it is because it has been deleted since and it should be listed in the OeTaskSourceRemover
                if (newFile != null) {
                    var finalFileTargets = newFile.GetAllTargets().ToList();
                    bool isFileWithTargetsToDelete = false;
                    var newCreateTargets = finalFileTargets.Select(t => t.GetTargetPath()).ToList();
                    foreach (var previousTarget in previousFile.Targets.ToNonNullList().Where(target => !target.IsDeletionMode())) {
                        var previousTargetPath = previousTarget.GetTargetPath();
                        if (!newCreateTargets.Exists(target => target.Equals(previousTargetPath, StringComparison.CurrentCultureIgnoreCase))) {
                            // the old target doesn't exist anymore, add it in deletion mode this time
                            isFileWithTargetsToDelete = true;
                            previousTarget.SetDeletionMode();
                            finalFileTargets.Add(previousTarget);
                        }
                    }
                    if (isFileWithTargetsToDelete) {
                        var previousFileCopy = (OeFileBuilt) Utils.DeepCopyPublicProperties(previousFile, typeof(OeFileBuilt));
                        previousFileCopy.Targets = finalFileTargets;
                        filesWithTargetsToDelete.Add(previousFileCopy);
                    }
                }
            }

            if (filesWithTargetsToDelete.Count > 0) {
                log?.Debug($"Added {filesWithTargetsToDelete.Count} files to the {typeof(OeTaskTargetsRemover).GetXmlName()} task because they have targets that no longer exist in the current build configuration, their old targets will be deleted");
                return new OeTaskTargetsRemover {
                    Label = "Deleting previous targets that no longer exist",
                    FilesWithTargetsToRemove = filesWithTargetsToDelete
                };
            }

            return null;
        }
    }
}