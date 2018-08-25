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

        public List<TaskExecutor> TaskExecutors { get; private set; } = new List<TaskExecutor>();
        
        public CancellationTokenSource CancelSource { get; } = new CancellationTokenSource();

        private string BuildTemporaryDirectory { get; set; }

        protected bool UseIncrementalBuild => BuildConfiguration.Properties.IncrementalBuildOptions.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled();

        private bool StoreSourceHash => BuildConfiguration.Properties.IncrementalBuildOptions?.StoreSourceHash ?? OeIncrementalBuildOptions.GetDefaultStoreSourceHash();
        
        private bool MirrorDeletedSourceFileToOutput => BuildConfiguration.Properties.IncrementalBuildOptions?.MirrorDeletedSourceFileToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput();
        
        private bool MirrorDeletedAndNewTargetsToOutput => BuildConfiguration.Properties.IncrementalBuildOptions?.MirrorDeletedAndNewTargetsToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedAndNewTargetsToOutput();

        protected string SourceDirectory => BuildConfiguration.Properties.BuildOptions?.SourceDirectoryPath;
        
        public bool FullRebuild => BuildConfiguration.Properties.BuildOptions?.FullRebuild ?? OeBuildOptions.GetDefaultFullRebuild();
        

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
            Exception ex = null;
            try {
                PreBuild();
                try {
                    Log?.Info($"Start building {BuildConfiguration}");
                    ExecuteBuildConfiguration();
                } catch (OperationCanceledException) {
                    Log?.Debug("Build canceled");
                    throw;
                }
            } catch (Exception e) {
                ex = e;
            } finally {
                try {
                    PostBuild();
                } catch (Exception e) {
                    if (ex == null) {
                        ex = e;
                    } else {
                        Log?.Error($"An error occured when analyzing the output of the build : {e.Message}");
                        Log?.Debug(e.ToString());
                    }
                }
            }
            if (ex != null) {
                throw new BuilderException(ex.Message, ex);
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
            ExecuteBuildStep<TaskExecutor>(BuildConfiguration.PreBuildTasks, nameof(OeBuildConfiguration.PreBuildTasks), null);
            ExecuteBuildStep<TaskExecutorWithFileListAndCompilation>(BuildConfiguration.BuildSourceTasks, nameof(OeBuildConfiguration.BuildSourceTasks), TaskExecutorConfiguratorBuildSource);
            ExecuteBuildStep<TaskExecutorWithFileList>(BuildConfiguration.BuildOutputTasks, nameof(OeBuildConfiguration.BuildOutputTasks), TaskExecutorConfiguratorBuildOutput);
            ExecuteBuildStep<TaskExecutor>(BuildConfiguration.PostBuildTasks, nameof(OeBuildConfiguration.PostBuildTasks), null);
        }

        /// <summary>
        /// Executes a build step
        /// </summary>
        private void ExecuteBuildStep<T>(IEnumerable<OeBuildStep> steps, string oeBuildConfigurationPropertyName, Action<T> taskExecutorConfigurator) where T : TaskExecutor, new() {
            var executionName = typeof(OeBuildConfiguration).GetXmlName(oeBuildConfigurationPropertyName);
            
            Log?.Debug($"Starting {executionName}");
            
            if (steps != null) {
                var i = 0;
                foreach (var step in steps) {
                    Log?.Debug($"Starting {executionName} - {step}");
                    var executor = new T {
                        Name = executionName,
                        Id = i,
                        Tasks = step.GetTaskList(),
                        Properties = BuildConfiguration.Properties,
                        Log = Log,
                        CancelSource = CancelSource
                    };
                    taskExecutorConfigurator?.Invoke(executor);
                    TaskExecutors.Add(executor);
                    executor.Execute();
                    i++;
                }
            }
        }

        private void TaskExecutorConfiguratorBuildOutput(TaskExecutorWithFileList executor) {
            var sourceLister = new SourceFilesLister(BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, CancelSource) {
                Log = Log
            };
            executor.TaskFiles = sourceLister.GetFileList();
        }

        private void TaskExecutorConfiguratorBuildSource(TaskExecutorWithFileListAndCompilation executor) {
            executor.TaskFiles = GetSourceFilesToRebuild(out List<OeFile> unfilteredSourceFilesList);

            List<IOeTask> extraRemoverTasks = null;
            
            if (MirrorDeletedSourceFileToOutput || MirrorDeletedAndNewTargetsToOutput) {
                Log?.Info("Mirroring deleted files in source to the output directory");
                var taskSourceRemover = GetTaskSourceRemover();
                if (taskSourceRemover != null) {
                    (extraRemoverTasks = new List<IOeTask>()).Add(taskSourceRemover);
                }
            }
            
            if (MirrorDeletedAndNewTargetsToOutput) {
                Log?.Info("Mirroring new and deleted targets to the output directory");
                var taskTargetsRemover = GetTaskTargetsRemover(executor, unfilteredSourceFilesList);
                if (taskTargetsRemover != null) {
                    (extraRemoverTasks ?? (extraRemoverTasks = new List<IOeTask>())).Add(taskTargetsRemover);
                }
            }

            executor.Tasks = executor.Tasks.UnionHandleNull(extraRemoverTasks);
        }
        
        private OeBuildHistory GetBuildHistory() {
            var history = new OeBuildHistory {
                BuiltFiles = GetFilesBuiltHistory(),
                CompilationProblems = new List<OeCompilationProblem>()
            };
            
            // add all compilation problems
            foreach (var file in TaskExecutors
                .SelectMany(exec => exec?.Tasks)
                .Where(task => task is IOeTaskCompile)
                .Cast<IOeTaskCompile>()
                .SelectMany(task => task.GetCompiledFiles().ToNonNullList())) {
                if (file.CompilationErrors == null || file.CompilationErrors.Count == 0) {
                    continue;
                }
                foreach (var problem in file.CompilationErrors) {
                    history.CompilationProblems.Add(OeCompilationProblem.New(file.SourceFilePath, problem));
                }
            }
            
            // TODO : webclient package info
            history.WebclientPackageInfo = null;

            return history;
        }
        
        /// <summary>
        /// Returns a list of all the files built; include files that were automatically deleted
        /// </summary>
        /// <returns></returns>
        private List<OeFileBuilt> GetFilesBuiltHistory() {
            
            var builtFiles = new Dictionary<string, OeFileBuilt>(StringComparer.CurrentCultureIgnoreCase);
            var outputDirectory = BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath;
            
            foreach (var fileBuilt in TaskExecutors
                .Where(te => te is TaskExecutorWithFileListAndCompilation)
                .Cast<TaskExecutorWithFileListAndCompilation>()
                .SelectMany(exec => exec?.Tasks)
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
        /// Gets all the files in the source directory that need to be rebuilt
        /// </summary>
        private List<OeFile> GetSourceFilesToRebuild(out List<OeFile> unfilteredSourceFilesList) {
            var sourceLister = GetSourceDirectoryFilesLister();
            
            unfilteredSourceFilesList = sourceLister.GetFileList();

            // in full rebuild, we build everything existing, we got this
            if (FullRebuild || !UseIncrementalBuild) {
                return unfilteredSourceFilesList;
            }
            
            // in incremental mode, we are not interested in the files that didn't change, we don't need to rebuild them
            var output = unfilteredSourceFilesList.Where(f => f.State != OeFileState.Unchanged).ToList();

            // add the files that need to be rebuild because of a dependency or table CRC modification
            if (PreviouslyBuiltFiles != null) {
                var extraFilesToRebuild = GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(BuildConfiguration.Properties.GetEnv(), output, PreviouslyBuiltFiles.Where(f => f is OeFileBuiltCompiled).Cast<OeFileBuiltCompiled>().ToList());
                foreach (var oeFile in sourceLister.FilterSourceFiles(extraFilesToRebuild)) {
                    if (!output.Exists(f => f.SourceFilePath.Equals(oeFile.SourceFilePath, StringComparison.CurrentCultureIgnoreCase)) && File.Exists(oeFile.SourceFilePath)) {
                        output.Add(oeFile);
                    }
                }
            }

            return output;
        }

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
        
        private IOeTask GetTaskSourceRemover() {
            if (PreviouslyBuiltFiles == null) {
                return null;
            }
            var deletedFileList = new List<OeFileBuilt>();
            foreach (var previousSourceFile in PreviouslyBuiltFiles.Where(f => f.State != OeFileState.Deleted)) {
                if (!File.Exists(previousSourceFile.SourceFilePath)) {
                    if (previousSourceFile.Targets != null && previousSourceFile.Targets.Count > 0) {
                        previousSourceFile.Targets.ForEach(target => target.SetDeletionMode());
                        deletedFileList.Add(previousSourceFile);
                    }
                }
            }
            if (deletedFileList.Count > 0) {
                Log?.Debug($"Added {deletedFileList.Count} files to the {typeof(OeTaskTargetsRemover).GetXmlName()} task because they no longer exist in the source directory, their targets will be deleted");
                return new OeTaskTargetsRemover {
                    Label = "Deleting files missing from the previous build",
                    FilesWithTargetsToRemove = deletedFileList
                };
            }
            return null;
        }

        private IOeTask GetTaskTargetsRemover(TaskExecutorWithFileListAndCompilation executor, List<OeFile> unfilteredSourceFilesList) {
            
            if (BuildConfiguration.Properties.SourceToBuildGitFilter != null) {
                Log?.Debug("We used a git filter for the source files, but we now need the complete list of files in the source directory, we do this now");
                // if we used a GIT filter, unfilteredSourceFilesList doesn't actually contain all the source files
                var sourceLister = GetSourceDirectoryFilesLister();
                sourceLister.SourcePathGitFilter = null;
                unfilteredSourceFilesList = sourceLister.GetFileList();
            }

            if (executor.Tasks != null) {
                Log?.Debug("Computing all the targets of all the files in the source directory");
                foreach (var task in executor.Tasks) {
                    TaskExecutor.SetFilesTargets(task, unfilteredSourceFilesList, BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath);
                }
            }
            
            Log?.Debug("Making a list of all unchanged files that have new targets since the last build as we will need to rebuild them");
            
            var filesWithNewTargets = new List<OeFile>();
            if (FullRebuild) {
                Log?.Debug("Full rebuild mode, we don't need to add files with new targets to the build list because they already are in the list");
            } else {
                foreach (var newFile in unfilteredSourceFilesList.Where(file => file.State == OeFileState.Unchanged)) {
                    var previousFile = PreviouslyBuiltFiles.First(prevFile => prevFile.SourceFilePath.Equals(newFile.SourceFilePath, StringComparison.CurrentCultureIgnoreCase));
                    var previouslyCreatedTargets = previousFile.Targets.ToNonNullList().Where(target => !target.IsDeletionMode()).Select(t => t.GetTargetPath()).ToList();
                    foreach (var targetPath in newFile.GetAllTargets().Select(t => t.GetTargetPath())) {
                        if (!previouslyCreatedTargets.Exists(prevTarget => prevTarget.Equals(targetPath, StringComparison.CurrentCultureIgnoreCase))) {
                            filesWithNewTargets.Add(newFile);
                            break;
                        }
                    }
                }
            }
            if (filesWithNewTargets.Count > 0) {
                Log?.Debug($"Added {filesWithNewTargets.Count} files to rebuild because they have new targets since the previous build");
                executor.TaskFiles.AddRange(filesWithNewTargets);
            }

            Log?.Debug("Making a list of all unchanged files that have new targets since the last build as we will need to rebuild them");
            
            var filesWithTargetsToDelete = new List<OeFileBuilt>();
            foreach (var previousFile in PreviouslyBuiltFiles.Where(file => file.State != OeFileState.Deleted)) {
                var newFile = unfilteredSourceFilesList.FirstOrDefault(nFile => nFile.SourceFilePath.Equals(previousFile.SourceFilePath, StringComparison.CurrentCultureIgnoreCase));
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
                        previousFile.Targets = finalFileTargets;
                        filesWithTargetsToDelete.Add(previousFile);
                    }
                }
            }

            if (filesWithTargetsToDelete.Count > 0) {
                Log?.Debug($"Added {filesWithTargetsToDelete.Count} files to the {typeof(OeTaskTargetsRemover).GetXmlName()} task because they have targets that no longer exist in the current build configuration, their old targets will be deleted");
                return new OeTaskTargetsRemover {
                    Label = "Deleting previous targets that no longer exist",
                    FilesWithTargetsToRemove = filesWithTargetsToDelete
                };
            }

            return null;
        }

        /// <summary>
        /// Returns a raw list of files that need to be rebuilt because :
        /// - one of their dependencies (source file, include) has been modified (modified/deleted)
        /// - one of their database references has been modified (modified/deleted)
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list
        /// </summary>
        /// <param name="env"></param>
        /// <param name="filesModified"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<OeFile> GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(UoeExecutionEnv env, IEnumerable<OeFile> filesModified, List<OeFileBuiltCompiled> previousFilesBuilt) {

            // add all previous source files that required now modified files
            foreach (var oeFile in filesModified) {
                foreach (var result in previousFilesBuilt.Where(prevf => prevf.RequiredFiles != null && prevf.RequiredFiles.Contains(oeFile.SourceFilePath, StringComparer.CurrentCultureIgnoreCase))) {
                    yield return result.GetDeepCopy();
                }
            }

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
    }
}