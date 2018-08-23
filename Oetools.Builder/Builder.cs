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
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

[assembly: InternalsVisibleTo("Oetools.Builder.Test")]

namespace Oetools.Builder {
    
    public class Builder : IDisposable {

        public ILogger Log { protected get; set; }

        public string SourceDirectory {
            get => _sourceDirectory;
            set =>_sourceDirectory = value.ToCleanPath();
        }
        
        public bool TestMode { get; set; }

        public bool FullRebuild {
            get => _forceFullRebuild || !UseIncrementalBuild;
            set => _forceFullRebuild = value;
        }
        
        public OeBuildConfiguration BuildConfiguration { get; }

        public OeBuildHistory BuildHistory { get; private set; }

        public UoeExecutionEnv Env { get; set; }

        public List<OeFileBuilt> PreviouslyBuiltFiles { get; set; }

        public List<TaskExecutor> PreBuildTaskExecutors { get; private set; }

        public List<TaskExecutorWithFileListAndCompilation> BuildSourceTaskExecutors { get; private set; }

        public List<TaskExecutorWithFileList> BuildOutputTaskExecutors { get; private set; }

        public List<TaskExecutor> PostBuildTaskExecutors { get; private set; }
        
        public CancellationTokenSource CancelSource { get; } = new CancellationTokenSource();

        protected bool UseIncrementalBuild => BuildConfiguration.Properties.IncrementalBuildOptions.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled();

        protected bool StoreSourceHash => BuildConfiguration.Properties.IncrementalBuildOptions?.StoreSourceHash ?? OeIncrementalBuildOptions.GetDefaultStoreSourceHash();
        
        private string _sourceDirectory;
        private bool _forceFullRebuild;

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
            if (string.IsNullOrEmpty(SourceDirectory)) {
                SourceDirectory = Directory.GetCurrentDirectory();
            }
            BuildConfiguration.Properties = BuildConfiguration.Properties ?? new OeProperties();
            BuildConfiguration.Properties.SetDefaultValues();
            BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath = BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath ?? OeBuilderConstants.GetDefaultOutputDirectory(SourceDirectory);
        }

        public void Dispose() {
            Utils.DeleteDirectoryIfExists(Env?.TempDirectory, true);
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
                    // TODO : handle this, maybe we want to do the post build actions anyway?
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
            Env = Env ?? BuildConfiguration.Properties.GetOeExecutionEnvironment(SourceDirectory, CancelSource);
            
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
            PreBuildTaskExecutors = ExecuteBuildStep<TaskExecutor>(BuildConfiguration.PreBuildTasks, nameof(OeBuildConfiguration.PreBuildTasks), null);
            
            BuildSourceTaskExecutors = ExecuteBuildStep<TaskExecutorWithFileListAndCompilation>(BuildConfiguration.BuildSourceTasks, nameof(OeBuildConfiguration.BuildSourceTasks), TaskExecutorConfiguratorBuildSource);
            
            BuildOutputTaskExecutors = ExecuteBuildStep<TaskExecutorWithFileList>(BuildConfiguration.BuildOutputTasks, nameof(OeBuildConfiguration.BuildOutputTasks), TaskExecutorConfiguratorBuildOutput);
            
            PostBuildTaskExecutors = ExecuteBuildStep<TaskExecutor>(BuildConfiguration.PostBuildTasks, nameof(OeBuildConfiguration.PostBuildTasks), null);
        }

        /// <summary>
        /// Executes a build step
        /// </summary>
        private List<T> ExecuteBuildStep<T>(IEnumerable<OeBuildStep> steps, string oeBuildConfigurationPropertyName, Action<T> taskExecutorConfigurator) where T : TaskExecutor, new() {
            var executionName = typeof(OeBuildConfiguration).GetXmlName(oeBuildConfigurationPropertyName);
            
            Log?.Debug($"Starting {executionName}");
            
            var output = new List<T>();
            if (steps != null) {
                var i = 0;
                foreach (var step in steps) {
                    
                    Log?.Debug($"Starting {executionName} - {step}");
                    
                    var executor = new T {
                        Name = executionName,
                        Id = i,
                        Tasks = step.GetTaskList(),
                        Properties = BuildConfiguration.Properties,
                        Env = Env,
                        Log = Log,
                        CancelSource = CancelSource
                    };
                    taskExecutorConfigurator?.Invoke(executor);
                    output.Add(executor);
                    executor.Execute();
                    i++;
                }
            }
            return output;
        }

        private void TaskExecutorConfiguratorBuildOutput(TaskExecutorWithFileList executor) {
            executor.OutputDirectory = BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath;
            var sourceLister = new SourceFilesLister(executor.OutputDirectory, CancelSource) {
                Log = Log
            };
            executor.TaskFiles = sourceLister.GetFileList();
        }

        private void TaskExecutorConfiguratorBuildSource(TaskExecutorWithFileListAndCompilation executor) {
            executor.OutputDirectory = BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath;
            executor.SourceDirectory = SourceDirectory;
            executor.TaskFiles = GetSourceFilesToRebuild();
            
            if (BuildConfiguration.Properties.IncrementalBuildOptions?.MirrorDeletedSourceFileToOutput ?? OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput()) {
                Log?.Info("Mirroring deleted files in source to the output directory");
                
                // we need to handle deleted files (files that existed in the previous build and that are missing now)
                // TODO : here we only "undo" files that were deleted from the source, but we don't handle the case of a file that changed targets
                // for instance, in the last build we deployed "myfile" to output/client/ and not the target for "myfile" is output/server/, we should delete it from client?
                var deletedFileList = GetDeletedFileList(PreviouslyBuiltFiles);
                if (deletedFileList != null && deletedFileList.Count > 0) {
                    var taskList = new List<IOeTask> {
                        new OeTaskSourceRemover {
                            Label = "Deleting files missing from the previous build",
                            FilesToRemove = deletedFileList
                        }
                    };
                    if (executor.Tasks != null) {
                        taskList.AddRange(executor.Tasks);
                    }
                    executor.Tasks = taskList;
                }
            }

        }
        
        private OeBuildHistory GetBuildHistory() {
            var history = new OeBuildHistory {
                BuiltFiles = GetFilesBuiltHistory(),
                CompilationProblems = new List<OeCompilationProblem>()
            };
            
            // add all compilation problems
            foreach (var file in BuildSourceTaskExecutors.SelectMany(exec => exec?.CompiledFiles.ToNonNullList())) {
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
            
            foreach (var fileBuilt in BuildSourceTaskExecutors
                .SelectMany(exec => exec?.Tasks)
                .Where(t => t is IOeTaskFileBuilder)
                .Cast<IOeTaskFileBuilder>()
                .SelectMany(t => t.GetFilesBuilt().ToNonNullList())) {
                
                // keep only the targets that are in the output directory, we won't be able to "undo" the others so it would be useless to keep them
                var targetsOutputDirectory = fileBuilt.Targets
                    .Where(t => t.GetTargetFilePath().StartsWith(outputDirectory, StringComparison.CurrentCultureIgnoreCase))
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
            if (StoreSourceHash) {
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
        private List<OeFile> GetSourceFilesToRebuild() {
            var sourceLister = new SourceFilesLister(SourceDirectory, CancelSource) {
                SourcePathFilter = BuildConfiguration.Properties.SourceToBuildPathFilter,
                SourcePathGitFilter = BuildConfiguration.Properties.SourceToBuildGitFilter,
                SetFileInfoAndState = UseIncrementalBuild,
                Log = Log
            };
            if (!FullRebuild) {
                sourceLister.PreviousSourceFiles = PreviouslyBuiltFiles;
                sourceLister.UseHashComparison = StoreSourceHash;
                sourceLister.UseLastWriteDateComparison = true;
            }
            
            var output = sourceLister.GetFileList();

            // in full rebuild, we build everything existing, we got this
            if (FullRebuild) {
                return output;
            }
            
            // in incremental mode, we are not interested in the files that didn't change, we don't need to rebuild them
            output = output.Where(f => f.State != OeFileState.Unchanged).ToList();

            // add the files that need to be rebuild because of a dependency or table CRC modification
            if (PreviouslyBuiltFiles != null) {
                var extraFilesToRebuild = GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(Env, output, PreviouslyBuiltFiles.Where(f => f is OeFileBuiltCompiled).Cast<OeFileBuiltCompiled>().ToList());
                foreach (var oeFile in sourceLister.FilterSourceFiles(extraFilesToRebuild)) {
                    if (!output.Exists(f => f.SourceFilePath.Equals(oeFile.SourceFilePath, StringComparison.CurrentCultureIgnoreCase)) && File.Exists(oeFile.SourceFilePath)) {
                        output.Add(oeFile);
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Returns a list of deleted files based on the <see cref="previousSourceFiles"/>
        /// </summary>
        /// <param name="previousSourceFiles"></param>
        /// <returns></returns>
        internal static List<OeFileBuilt> GetDeletedFileList(IEnumerable<OeFileBuilt> previousSourceFiles) {
            var output = new List<OeFileBuilt>();
            if (previousSourceFiles == null) {
                return output;
            }
            foreach (var previousSourceFile in previousSourceFiles.Where(f => f.State != OeFileState.Deleted)) {
                if (!File.Exists(previousSourceFile.SourceFilePath)) {
                    output.Add(previousSourceFile);
                }
            }
            return output;
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