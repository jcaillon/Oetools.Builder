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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

[assembly: InternalsVisibleTo("Oetools.Builder.Test")]

namespace Oetools.Builder {
    
    public class Builder {
        
        private string _sourceDirectory;
        private bool _forceFullRebuild;

        protected ILogger Log { get; set; }

        public string SourceDirectory {
            get => _sourceDirectory;
            set =>_sourceDirectory = value.ToCleanPath();
        }

        public OeBuildConfiguration BuildConfiguration { get; }
        
        public bool TestMode { get; set; }

        public bool ForceFullRebuild {
            get => _forceFullRebuild || NoIncrementalBuild;
            set => _forceFullRebuild = value;
        }

        public bool NoIncrementalBuild => BuildConfiguration?.Properties?.IncrementalBuildOptions?.Disabled ?? OeIncrementalBuildOptions.GetDefaultDisabled();

        public EnvExecution Env { get; private set; }
        
        public List<OeFileBuilt> PreviouslyBuiltFiles { get; set; }
        
        public List<TaskExecutor> PreBuildTaskExecutors { get; set; }
        
        public List<TaskExecutorBuildingSource> BuildSourceTaskExecutors { get; set; }
        
        public List<TaskExecutor> BuildOutputTaskExecutors { get; set; }
        
        public List<TaskExecutor> PostBuildTaskExecutors { get; set; }

        /// <summary>
        /// Initiliaze the build
        /// </summary>
        /// <param name="project"></param>
        /// <param name="buildConfigurationName"></param>
        public Builder(OeProject project, string buildConfigurationName = null) {
            // make a copy of the build configuration
            BuildConfiguration = project.GetBuildConfigurationCopy(buildConfigurationName) ?? project.GetDefaultBuildConfigurationCopy();
            
        }
        
        /// <summary>
        /// Main method, builds
        /// </summary>
        public void Build() {
            Log.Debug($"Initializing build with {BuildConfiguration}");
            Init();
            
            Log.Info($"Start building {(string.IsNullOrEmpty(BuildConfiguration.ConfigurationName) ? "an unnamed configuration" : $"the configuration {BuildConfiguration.ConfigurationName}")}");

            Log.Debug("Validating tasks");
            BuildConfiguration.ValidateAllTasks();
            
            Log.Debug("Using build variables");
            BuildConfiguration.ApplyVariables(SourceDirectory);
            
            Log.Debug("Sanitizing path properties");
            BuildConfiguration.Properties.SanitizePathInPublicProperties();
            
            ExecuteBuild();
        }

        private void Init() {
            
            Env = new EnvExecution {
                TempDirectory = BuildConfiguration?.Properties?.TemporaryDirectoryPath?.TakeDefaultIfNeeded($".oe_tmp-{Utils.GetRandomName()}"),
                UseProgressCharacterMode = BuildConfiguration?.Properties?.UseCharacterModeExecutable ?? OeProjectProperties.GetDefaultUseCharacterModeExecutable(),
                DatabaseAliases = BuildConfiguration?.Properties?.DatabaseAliases,
                // TODO : create + start a database!
                DatabaseConnectionString = BuildConfiguration?.Properties?.DatabaseConnectionExtraParameters,
                DatabaseConnectionStringAppendMaxTryOne = true,
                DlcDirectoryPath = BuildConfiguration?.Properties?.DlcDirectoryPath.TakeDefaultIfNeeded(OeProjectProperties.GetDefaultDlcDirectoryPath()),
                IniFilePath = BuildConfiguration?.Properties?.IniFilePath,
                PostExecutionProgramPath = BuildConfiguration?.Properties?.ProcedurePathToExecuteAfterAnyProgressExecution,
                PreExecutionProgramPath = BuildConfiguration?.Properties?.ProcedurePathToExecuteBeforeAnyProgressExecution,
                ProExeCommandLineParameters = BuildConfiguration?.Properties?.ProgresCommandLineExtraParameters,
                ProPathList = BuildConfiguration?.Properties?.GetPropath(SourceDirectory, true)
            };
        }

        private void ExecuteBuild() {           
            if (BuildConfiguration.PostBuildTasks != null) {
                var i = 0;
                foreach (var step in BuildConfiguration.PostBuildTasks) {
                    Log.Debug($"{typeof(OeBuildConfiguration).GetXmlName(nameof(OeBuildConfiguration.PostBuildTasks))} step {i}{(!string.IsNullOrEmpty(step.Label) ? $" : {step.Label}" : "")}");
                    var executor = new TaskExecutor(step.GetTaskList());
                    (PostBuildTaskExecutors ?? (PostBuildTaskExecutors = new List<TaskExecutor>())).Add(executor);
                    executor.ProjectProperties = BuildConfiguration.Properties;
                    executor.Env = Env;
                    executor.Execute();
                }
            }
        }
        
        /// <summary>
        /// Sets <see cref="TaskExecutorWithFileList.TaskFiles"/> to the list of source files that need to be rebuilt
        /// </summary>
        internal List<OeFile> GetSourceFilesToRebuild() {
            var sourceLister = new SourceFilesLister(SourceDirectory) {
                SourcePathFilter = BuildConfiguration?.Properties?.SourcePathFilter,
                SourcePathGitFilter = BuildConfiguration?.Properties?.SourcePathGitFilter
            };
            if (!NoIncrementalBuild) {
                sourceLister.PreviousSourceFiles = PreviouslyBuiltFiles;
                sourceLister.UseHashComparison = BuildConfiguration?.Properties?.IncrementalBuildOptions?.StoreSourceHash ?? OeIncrementalBuildOptions.GetDefaultStoreSourceHash();
                sourceLister.UseLastWriteDateComparison = true;
            }
            var output = sourceLister.GetFileList();
            var extraFilesToRebuild = GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(Env, output, PreviouslyBuiltFiles.Where(f => f is OeFileBuiltCompiled).Cast<OeFileBuiltCompiled>().ToList());
            foreach (var oeFile in sourceLister.FilterSourceFiles(extraFilesToRebuild)) {
                if (!output.Exists(f => f.SourcePath.Equals(oeFile.SourcePath, StringComparison.CurrentCultureIgnoreCase)) && 
                    File.Exists(oeFile.SourcePath)) {
                    output.Add(oeFile);
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
        internal static IEnumerable<OeFile> GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(EnvExecution env, IEnumerable<OeFile> filesModified, List<OeFileBuiltCompiled> previousFilesBuilt) {

            // add all previous source files that required now modified files
            foreach (var oeFile in filesModified) {
                foreach (var result in previousFilesBuilt.Where(prevf => prevf.RequiredFiles != null && prevf.RequiredFiles.Contains(oeFile.SourcePath, StringComparer.CurrentCultureIgnoreCase))) {
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