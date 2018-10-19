#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeProperties.cs) is part of Oetools.Builder.
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project {
    
    /// <remarks>
    /// Every public property string not marked with the <see cref="ReplaceVariables"/> attribute is allowed
    /// to use &lt;VARIABLE&gt; which will be replace at the beginning of the build by <see cref="OeBuildConfiguration.Variables"/>
    /// </remarks>
    [Serializable]
    public class OeProperties {

        [Description(@"The path to the directory containing the installation of openedge. 
Commonly known as the DLC directory.")]
        [XmlElement(ElementName = "DlcDirectoryPath")]
        public string DlcDirectoryPath { get; set; }
        [Description("$DLC (openedge installation directory)")]
        public static string GetDefaultDlcDirectoryPath() => UoeUtilities.GetDlcPathFromEnv().ToCleanPath();
        
        [Description(@"A list of all the openedge databases used by your project (logical name + data definition). 
This list should contain all the databases necessary to compile your application.
The databases specified within this option will be automatically generated during a build.
The idea is to only have to version .df files (data definition files)")]
        [XmlArray("ProjectDatabases")]
        [XmlArrayItem("ProjectDatabase", typeof(OeProjectDatabase))]
        public List<OeProjectDatabase> ProjectDatabases { get; set; }
        
        [Description(@"A database connection string that will be used to connect to extra databases before a build. 
This obviously requires existing databases. 
Most of the time, it is simpler to use the ProjectDatabase option instead of this one.")]
        [XmlElement(ElementName = "DatabaseConnectionExtraParameters")]
        public string DatabaseConnectionExtraParameters { get; set; }

        [XmlArray("DatabaseAliases")]
        [XmlArrayItem("Alias", typeof(OeDatabaseAlias))]
        public List<OeDatabaseAlias> DatabaseAliases { get; set; }           
            
        [XmlElement(ElementName = "IniFilePath")]
        public string IniFilePath { get; set; }

        [XmlArrayItem("Path", typeof(string))]
        [XmlArray("PropathEntries")]
        public List<string> PropathEntries { get; set; }
        
        [XmlElement(ElementName = "AddAllSourceDirectoriesToPropath")]
        public bool? AddAllSourceDirectoriesToPropath { get; set; }
        public static bool GetDefaultAddAllSourceDirectoriesToPropath() => true;
            
        [XmlElement(ElementName = "PropathSourceDirectoriesFilter")]
        public OeTaskFilter PropathSourceDirectoriesFilter { get; set; }

        /// <summary>
        /// Adds the gui or tty (depending on <see cref="UseCharacterModeExecutable"/>) folder as well as the contained .pl to the propath
        /// Also adds dlc and dlc/bin
        /// </summary>
        [XmlElement(ElementName = "AddDefaultOpenedgePropath")]
        public bool? AddDefaultOpenedgePropath { get; set; }
        public static bool GetDefaultAddDefaultOpenedgePropath() => true;

        [XmlElement(ElementName = "UseCharacterModeExecutable")]
        public bool? UseCharacterModeExecutable { get; set; }
        public static bool GetDefaultUseCharacterModeExecutable() => false;

        [XmlElement(ElementName = "OpenedgeCommandLineExtraParameters")]
        public string OpenedgeCommandLineExtraParameters { get; set; }

        [XmlElement(ElementName = "ProcedurePathToExecuteBeforeAnyProgressExecution")]
        public string ProcedurePathToExecuteBeforeAnyProgressExecution { get; set; }

        [XmlElement(ElementName = "ProcedurePathToExecuteAfterAnyProgressExecution")]
        public string ProcedurePathToExecuteAfterAnyProgressExecution { get; set; }

        [XmlElement(ElementName = "OpenedgeTemporaryDirectoryPath")]
        public string OpenedgeTemporaryDirectoryPath { get; set; }
        [Description("$TEMP/.oe_tmp-xxx (temporary folder)")]
        public static string GetDefaultOpenedgeTemporaryDirectoryPath() => Path.Combine(Path.GetTempPath(), $".oe_tmp-{Utils.GetRandomName()}");
        
        /// <summary>
        /// Allows to exclude path from being treated by <see cref="OeBuildConfiguration.BuildSourceStepGroup"/>
        /// Specify what should not be considered as a source file in your source directory (for instance, the docs/ folder)
        /// </summary>
        [XmlElement(ElementName = "SourceToBuildPathFilter")]
        public OeTaskFilter SourceToBuildPathFilter { get; set; }
                
        /// <summary>
        /// Use this to apply GIT filters to your <see cref="OeBuildConfiguration.BuildSourceStepGroup"/>
        /// Obviously, you need GIT installed and present in your OS path
        /// </summary>
        [XmlElement(ElementName = "GitFilterBuildOptions")]
        public OeGitFilterBuildOptions GitFilterBuildOptions { get; set; }    
        [Description("")]
        public static OeGitFilterBuildOptions GetDefaultGitFilterBuildOptions() => new OeGitFilterBuildOptions();
                  
        [XmlElement(ElementName = "CompilationOptions")]
        public OeCompilationOptions CompilationOptions { get; set; }
        [Description("")]
        public static OeCompilationOptions GetDefaultCompilationOptions() => new OeCompilationOptions();
            
        [XmlElement(ElementName = "IncrementalBuildOptions")]
        public OeIncrementalBuildOptions IncrementalBuildOptions { get; set; }
        [Description("")]
        public static OeIncrementalBuildOptions GetDefaultIncrementalBuildOptions() => new OeIncrementalBuildOptions();
        
        [XmlElement(ElementName = "BuildOptions")]
        public OeBuildOptions BuildOptions { get; set; }
        [Description("")]
        public static OeBuildOptions GetDefaultBuildOptions() => new OeBuildOptions();

        /// <summary>
        /// Sets default values to all the properties (and recursively) of this object, using the GetDefault... methods
        /// </summary>
        public void SetDefaultValues() {
            Utils.SetDefaultValues(this);
        }
        
        /// <summary>
        /// Validate that is object is correct
        /// </summary>
        /// <exception cref="BuildConfigurationException"></exception>
        public void Validate() {
            ValidateFilters(PropathSourceDirectoriesFilter, nameof(PropathSourceDirectoriesFilter));
            ValidateFilters(SourceToBuildPathFilter, nameof(SourceToBuildPathFilter));
            if ((CompilationOptions?.NumberProcessPerCore ?? 0) > 10) {
                throw new PropertiesException($"The property {typeof(OeCompilationOptions).GetXmlName(nameof(OeCompilationOptions.NumberProcessPerCore))} should not exceed 10");
            }

            if ((IncrementalBuildOptions?.IsActive() ?? false) && (GitFilterBuildOptions?.IsActive() ?? false)) {
                throw new PropertiesException($"The {GetType().GetXmlName(nameof(IncrementalBuildOptions))} can not be active when the {GetType().GetXmlName(nameof(GitFilterBuildOptions))} is active because the two options serve contradictory purposes. {GetType().GetXmlName(nameof(IncrementalBuildOptions))} should be used when the goal is to build the latest modifications on top of a previous build. {GetType().GetXmlName(nameof(GitFilterBuildOptions))} should be used when the goal is to verify that recent commits to the git repo did not introduce bugs.");
            }
            if (!(IncrementalBuildOptions?.IsActive() ?? false) && (IncrementalBuildOptions?.FullRebuild ?? OeIncrementalBuildOptions.GetDefaultFullRebuild())) {
                throw new PropertiesException($"In {GetType().GetXmlName(nameof(IncrementalBuildOptions))}, the property {typeof(OeBuildOptions).GetXmlName(nameof(OeIncrementalBuildOptions.FullRebuild))} can only be set to true if the property {typeof(OeIncrementalBuildOptions).GetXmlName(nameof(OeIncrementalBuildOptions.Enabled))} is set to true");
            }
        }
        
        private void ValidateFilters(OeTaskFilter filter, string propertyNameOf) {
            try {
                filter?.Validate();
            } catch (Exception e) {
                throw new PropertiesException($"Filter property {propertyNameOf} : {e.Message}", e);
            }
        }

        /// <summary>
        /// Clean the path of all path properties
        /// </summary>
        public void SanitizePathInPublicProperties() {
            Utils.ForEachPublicPropertyStringInObject(typeof(OeProperties), this, (propInfo, value) => {
                if (!propInfo.Name.Contains("Path")) {
                    return value;
                }
                if (string.IsNullOrEmpty(value)) {
                    return value;
                }
                return value.ToCleanPath();
            });
        }

        /// <summary>
        /// Returns the propath that should be used considering all the options of this class
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="simplifyPathWithWorkingDirectory"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public PathList<OeDirectory> GetPropath(string sourceDirectory, bool simplifyPathWithWorkingDirectory) {
            var output = new PathList<OeDirectory>();
            if (PropathEntries != null) {
                foreach (var propathEntry in PropathEntries) {
                    var entry = propathEntry.ToCleanPath();
                    try {
                        // need to take into account relative paths
                        if (!Path.IsPathRooted(entry)) {
                            entry = Path.GetFullPath(Path.Combine(sourceDirectory, entry));
                        }

                        if (!Directory.Exists(entry) && !File.Exists(entry)) {
                            continue;
                        }
                    } catch (Exception) {
                        continue;
                    }

                    output.TryAdd(new OeDirectory(entry));
                }
            }
            // read from ini
            if (!string.IsNullOrEmpty(IniFilePath)) {
                foreach (var entry in UoeUtilities.GetProPathFromIniFile(IniFilePath, sourceDirectory)) {
                    output.TryAdd(new OeDirectory(entry));
                }
            }
            if (AddAllSourceDirectoriesToPropath ?? GetDefaultAddAllSourceDirectoriesToPropath() && Directory.Exists(sourceDirectory)) {
                var lister = new PathLister(sourceDirectory, _cancelToken) {
                    PathFilter = PropathSourceDirectoriesFilter
                };
                output.TryAddRange(lister.GetDirectoryList());
            }
            if (AddDefaultOpenedgePropath ?? GetDefaultAddDefaultOpenedgePropath()) {
                // %DLC%/tty or %DLC%/gui + %DLC% + %DLC%/bin
                foreach (var file in UoeUtilities.GetProgressSessionDefaultPropath(DlcDirectoryPath.TakeDefaultIfNeeded(GetDefaultDlcDirectoryPath()), UseCharacterModeExecutable ?? GetDefaultUseCharacterModeExecutable())) {
                    output.TryAdd(new OeDirectory(file));
                }
            }
            if (simplifyPathWithWorkingDirectory) {
                output.ApplyPathTransformation(d => {
                    d.Path = d.Path.FromAbsolutePathToRelativePath(sourceDirectory);
                    return d;
                });
            }
            return output;
        }


        private CancellationToken? _cancelToken;

        /// <summary>
        /// Sets the cancellation source used in this class for long operations (like <see cref="GetPropath"/>)
        /// </summary>
        /// <param name="source"></param>
        public void SetCancellationSource(CancellationToken? source) => _cancelToken = source;

        private UoeExecutionEnv _env;

        /// <summary>
        /// Get the execution environment from these properties
        /// </summary>
        public UoeExecutionEnv GetEnv() {
            if (_env == null) {
                _env = new UoeExecutionEnv {
                    TempDirectory = OpenedgeTemporaryDirectoryPath.TakeDefaultIfNeeded(GetDefaultOpenedgeTemporaryDirectoryPath()),
                    UseProgressCharacterMode = UseCharacterModeExecutable ?? GetDefaultUseCharacterModeExecutable(),
                    DatabaseAliases = DatabaseAliases,
                    DatabaseConnectionString = DatabaseConnectionExtraParameters,
                    DatabaseConnectionStringAppendMaxTryOne = true,
                    DlcDirectoryPath = DlcDirectoryPath.TakeDefaultIfNeeded(GetDefaultDlcDirectoryPath()),
                    IniFilePath = IniFilePath,
                    PostExecutionProgramPath = ProcedurePathToExecuteAfterAnyProgressExecution,
                    PreExecutionProgramPath = ProcedurePathToExecuteBeforeAnyProgressExecution,
                    ProExeCommandLineParameters = OpenedgeCommandLineExtraParameters,
                    ProPathList = ((IEnumerable<OeDirectory>) GetPropath((BuildOptions?.SourceDirectoryPath).TakeDefaultIfNeeded(OeBuildOptions.GetDefaultSourceDirectoryPath()), true)).Select(d => d.Path).ToList()
                };
            }
            return _env;
        }
        
        public void SetEnv(UoeExecutionEnv value) => _env = value;

        /// <summary>
        /// Get the parallel compiler from these properties
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        public UoeExecutionParallelCompile GetParallelCompiler(string workingDirectory) =>
            new UoeExecutionParallelCompile(GetEnv()) {
                CompileInAnalysisMode = IncrementalBuildOptions?.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled(),
                WorkingDirectory = workingDirectory,
                NeedDatabaseConnection = true,
                AnalysisModeSimplifiedDatabaseReferences = CompilationOptions?.UseSimplerAnalysisForDatabaseReference ?? OeCompilationOptions.GetDefaultUseSimplerAnalysisForDatabaseReference(),
                CompileOptions = CompilationOptions?.CompileOptions,
                CompilerMultiCompile = CompilationOptions?.UseCompilerMultiCompile ?? OeCompilationOptions.GetDefaultUseCompilerMultiCompile(),
                CompileStatementExtraOptions = CompilationOptions?.CompileStatementExtraOptions,
                CompileUseXmlXref = CompilationOptions?.CompileWithXmlXref ?? OeCompilationOptions.GetDefaultCompileWithXmlXref(),
                CompileWithDebugList = CompilationOptions?.CompileWithDebugList ?? OeCompilationOptions.GetDefaultCompileWithDebugList(),
                CompileWithListing = CompilationOptions?.CompileWithListing ?? OeCompilationOptions.GetDefaultCompileWithListing(),
                CompileWithPreprocess = CompilationOptions?.CompileWithPreprocess ?? OeCompilationOptions.GetDefaultCompileWithPreprocess(),
                CompileWithXref = CompilationOptions?.CompileWithXref ?? OeCompilationOptions.GetDefaultCompileWithXref(),
                MaxNumberOfProcesses = OeCompilationOptions.GetNumberOfProcessesToUse(CompilationOptions),
                MinimumNumberOfFilesPerProcess = CompilationOptions?.MinimumNumberOfFilesPerProcess ?? OeCompilationOptions.GetDefaultMinimumNumberOfFilesPerProcess(),
                StopOnCompilationError = BuildOptions?.StopBuildOnCompilationError ?? OeBuildOptions.GetDefaultStopBuildOnCompilationError(),
                StopOnCompilationWarning = BuildOptions?.StopBuildOnCompilationWarning ?? OeBuildOptions.GetDefaultStopBuildOnCompilationWarning()
            };
    }
}