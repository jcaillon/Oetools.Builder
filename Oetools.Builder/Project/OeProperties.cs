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
    
    /// <inheritdoc cref="OeBuildConfiguration.Properties"/>
    /// <code>
    /// Every public property string not marked with the <see cref="ReplaceVariables"/> attribute is allowed
    /// to use {{VARIABLE}} which will be replace at the beginning of the build by <see cref="OeBuildConfiguration.Variables"/>
    /// </code>
    [Serializable]
    public class OeProperties {

        /// <summary>
        /// The path to the directory containing the installation of openedge.
        /// Commonly known as the DLC directory.
        /// </summary>
        /// <remarks>
        /// It should contain, among many other things, a "bin" directory where the openedge executables are located.
        /// </remarks>
        [XmlElement(ElementName = "DlcDirectory")]
        public string DlcDirectory { get; set; }
        [Description("$DLC (openedge installation directory)")]
        public static string GetDefaultDlcDirectory() => UoeUtilities.GetDlcPathFromEnv().ToCleanPath();
        
        /// <summary>
        /// The code page to use for input/output with openedge processes.
        /// This will default to the value read for -cpstream or -cpinternal in the file $DLC/startup.pf.
        /// </summary>
        /// <remarks>
        /// This property should be configured if you encounter wrong characters (wrong encoding) in the console.
        /// </remarks>
        [XmlElement(ElementName = "OpenedgeCodePage")]
        public string OpenedgeCodePage { get; set; }
        
        /// <summary>
        /// A list of all the openedge databases used by your project (couple of logical name + data definition file path).
        /// This list should contain all the databases necessary to compile your application.
        /// </summary>
        /// <remarks>
        /// The databases specified in this property will be automatically generated during a build (temporary databases).
        /// The idea is to version .df files (data definition files) and to have a fully buildable application from a checkout.
        /// </remarks>
        [XmlArray("ProjectDatabases")]
        [XmlArrayItem("ProjectDatabase", typeof(OeProjectDatabase))]
        public List<OeProjectDatabase> ProjectDatabases { get; set; }
        
        /// <summary>
        /// A database connection string that will be used to connect to extra databases before a build. 
        /// </summary>
        /// <remarks>
        /// This obviously requires existing databases.
        /// Most of the time, it is simpler to use the ProjectDatabase option instead of this one and let the tool generate the necessary databases.
        /// This property is used in the following openedge statement: CONNECT VALUE(this_property).
        /// </remarks>
        /// <example>
        /// -db base1 -ld mylogicalName1 -H 127.0.0.1 -S 1024
        /// -db C:\wrk\sport2000.db -1 -ld mydb
        /// -pf C:\mypath\db.pf
        /// </example>
        [XmlElement(ElementName = "ExtraDatabaseConnectionString")]
        public string ExtraDatabaseConnectionString { get; set; }

        /// <summary>
        /// A list of database aliases needed in your project (couple of logical name + alias name).
        /// </summary>
        /// <remarks>
        /// This is useful when your code references several aliases of a single database.
        /// </remarks>
        [XmlArray("DatabaseAliases")]
        [XmlArrayItem("Alias", typeof(OeDatabaseAlias))]
        public List<OeDatabaseAlias> DatabaseAliases { get; set; }           
            
        /// <summary>
        /// The path to the .ini file used by your project.
        /// </summary>
        /// <remarks>
        /// .ini files are specific to the windows platform.
        /// .ini files are typically used to define COLORS and FONTS for GUI applications.
        /// The font definition are required to correctly compiled .w files.
        /// The section [STARTUP] and key [PROPATH] is read and appended to the compilation propath.
        /// It is advised to version a neutral .ini file with a blank propath in order to allow the compilation of a GUI application.
        /// Relative path are resolved with the current directory but you can use {{SOURCE_DIRECTORY}} to target the source directory.
        /// </remarks>
        [XmlElement(ElementName = "IniFilePath")]
        public string IniFilePath { get; set; }

        /// <summary>
        /// A list of paths to add to the propath during the build.
        /// </summary>
        /// <remarks>
        /// This list should include all the directories containing the include files necessary to compile your application.
        /// This typically include pro library (.pl) file path or directories.
        /// Relative path are resolved with the current directory but you can use {{SOURCE_DIRECTORY}} to target the source directory.
        /// </remarks>
        [XmlArrayItem("Path", typeof(string))]
        [XmlArray("PropathEntries")]
        public List<string> PropathEntries { get; set; }
        
        /// <summary>
        /// Indicates if all the directories in the source directory should be added to the compilation propath.
        /// </summary>
        /// <remarks>
        /// The idea is to use this option instead of manually specifying the compilation propath (for lazy people only).
        /// </remarks>
        [XmlElement(ElementName = "AddAllSourceDirectoriesToPropath")]
        public bool? AddAllSourceDirectoriesToPropath { get; set; }
        public static bool GetDefaultAddAllSourceDirectoriesToPropath() => true;
            
        /// <summary>
        /// The filtering options for the automatic listing of directories in the source directory (to use as propath).
        /// </summary>
        [XmlElement(ElementName = "PropathSourceDirectoriesFilter")]
        public OeFilterOptions PropathSourceDirectoriesFilter { get; set; }

        /// <summary>
        /// Force the use of the openedge character mode on windows platform.
        /// </summary>
        /// <remarks>
        /// Typically, this option will make the build process use the "_progres.exe" executable instead of "prowin.exe".
        /// </remarks>
        [XmlElement(ElementName = "UseCharacterModeExecutable")]
        public bool? UseCharacterModeExecutable { get; set; }
        public static bool GetDefaultUseCharacterModeExecutable() => false;
        
        /// <summary>
        /// Adds the gui (or tty if character mode) directory to the propath.
        /// Also adds the pro library files in this directory to the propath.
        /// Also adds the dlc and dlc/bin directories.
        /// </summary>
        /// <remarks>
        /// This is the equivalent of the default propath set by openedge.
        /// </remarks>
        [XmlElement(ElementName = "AddDefaultOpenedgePropath")]
        public bool? AddDefaultOpenedgePropath { get; set; }
        public static bool GetDefaultAddDefaultOpenedgePropath() => true;

        /// <summary>
        /// Command line parameters to add when using the openedge executable (_progres or prowin).
        /// The available parameters for your version of openedge are available in the reference help, topic "Startup Parameter Descriptions".
        /// </summary>
        /// <example>
        /// -inp 9999
        /// -s 500
        /// -assemblies "/root/assemblies/"
        /// -NL -cwl -k
        /// </example>
        [XmlElement(ElementName = "ExtraOpenedgeCommandLineParameters")]
        public string ExtraOpenedgeCommandLineParameters { get; set; }

        /// <summary>
        /// File path to an openedge procedure that will be executed for each new openedge session used (when using _progres or prowin).
        /// </summary>
        /// <remarks>
        /// This procedure is called with a simple RUN statement and must not have any parameters.
        /// </remarks>
        /// <example>
        /// This feature can be used to connected databases, create aliases or add paths to the propath before a compilation and using a custom logic.
        /// </example>
        [XmlElement(ElementName = "ProcedureToExecuteBeforeAnyProgressExecutionFilePath")]
        public string ProcedureToExecuteBeforeAnyProgressExecutionFilePath { get; set; }

        /// <summary>
        /// File path to an openedge procedure that will be executed at the end of each new openedge session used (when using _progres or prowin).
        /// </summary>
        /// <remarks>
        /// This procedure is called with a simple RUN statement and must not have any parameters.
        /// It should be used to "clean up" any custom logic put in place with the procedure execution at the beginning of the session.
        /// </remarks>
        [XmlElement(ElementName = "ProcedurePathToExecuteAfterAnyProgressExecution")]
        public string ProcedureToExecuteAfterAnyProgressExecutionFilePath { get; set; }

        /// <summary>
        /// The temporary directory to use for an openedge session.
        /// </summary>
        /// <remarks>
        /// This is the directory used in the -T startup parameter for the openedge session.
        /// This directory is also used to store temporary files needed for the compilation and for the interface between openedge and this tool.
        /// </remarks>
        [XmlElement(ElementName = "OpenedgeTemporaryDirectory")]
        public string OpenedgeTemporaryDirectory { get; set; }
        [Description("$TEMP/.oe_tmp-xxx (temporary folder)")]
        public static string GetDefaultOpenedgeTemporaryDirectory() => Path.Combine(Path.GetTempPath(), $".oe_tmp-{Utils.GetRandomName()}");
                  
        /// <inheritdoc cref="OeCompilationOptions"/>
        [XmlElement(ElementName = "CompilationOptions")]
        public OeCompilationOptions CompilationOptions { get; set; }
        [Description("")]
        public static OeCompilationOptions GetDefaultCompilationOptions() => new OeCompilationOptions();
        
        /// <inheritdoc cref="OeBuildOptions"/>
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
            if ((CompilationOptions?.NumberProcessPerCore ?? 0) > 10) {
                throw new PropertiesException($"The property {typeof(OeCompilationOptions).GetXmlName(nameof(OeCompilationOptions.NumberProcessPerCore))} should not exceed 10.");
            }
            if (BuildOptions != null) {
                ValidateFilters(BuildOptions.SourceToBuildFilter, nameof(BuildOptions.SourceToBuildFilter));
                if ((BuildOptions?.IncrementalBuildOptions?.IsActive() ?? false) && (BuildOptions.SourceToBuildGitFilter?.IsActive() ?? false)) {
                    throw new PropertiesException($"The {GetType().GetXmlName(nameof(BuildOptions.IncrementalBuildOptions))} can not be active when the {GetType().GetXmlName(nameof(BuildOptions.SourceToBuildGitFilter))} is active because the two options serve contradictory purposes. {GetType().GetXmlName(nameof(BuildOptions.IncrementalBuildOptions))} should be used when the goal is to build the latest modifications on top of a previous build. {GetType().GetXmlName(nameof(BuildOptions.SourceToBuildGitFilter))} should be used when the goal is to verify that recent commits to the git repo did not introduce bugs.");
                }
            }
        }
        
        private void ValidateFilters(AOeTaskFilter filter, string propertyNameOf) {
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
        public PathList<IOeDirectory> GetPropath(string sourceDirectory, bool simplifyPathWithWorkingDirectory) {
            var currentDirectory = Directory.GetCurrentDirectory();
            
            var output = new PathList<IOeDirectory>();
            if (PropathEntries != null) {
                foreach (var propathEntry in PropathEntries) {
                    var entry = propathEntry.ToCleanPath();
                    try {
                        // need to take into account relative paths
                        if (!Path.IsPathRooted(entry)) {
                            entry = Path.GetFullPath(Path.Combine(currentDirectory, entry));
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
                foreach (var entry in UoeUtilities.GetProPathFromIniFile(IniFilePath, currentDirectory)) {
                    output.TryAdd(new OeDirectory(entry));
                }
            }
            if (AddAllSourceDirectoriesToPropath ?? GetDefaultAddAllSourceDirectoriesToPropath() && Directory.Exists(sourceDirectory)) {
                var lister = new PathLister(sourceDirectory, _cancelToken) {
                    FilterOptions = PropathSourceDirectoriesFilter
                };
                output.TryAddRange(lister.GetDirectoryList());
            }
            if (AddDefaultOpenedgePropath ?? GetDefaultAddDefaultOpenedgePropath()) {
                // %DLC%/tty or %DLC%/gui + %DLC% + %DLC%/bin
                foreach (var file in UoeUtilities.GetProgressSessionDefaultPropath(DlcDirectory.TakeDefaultIfNeeded(GetDefaultDlcDirectory()), UseCharacterModeExecutable ?? GetDefaultUseCharacterModeExecutable())) {
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
                    TempDirectory = OpenedgeTemporaryDirectory.TakeDefaultIfNeeded(GetDefaultOpenedgeTemporaryDirectory()),
                    UseProgressCharacterMode = UseCharacterModeExecutable ?? GetDefaultUseCharacterModeExecutable(),
                    DatabaseAliases = DatabaseAliases,
                    DatabaseConnectionString = ExtraDatabaseConnectionString,
                    DatabaseConnectionStringAppendMaxTryOne = true,
                    DlcDirectoryPath = DlcDirectory.TakeDefaultIfNeeded(GetDefaultDlcDirectory()),
                    IniFilePath = IniFilePath,
                    PostExecutionProgramPath = ProcedureToExecuteAfterAnyProgressExecutionFilePath,
                    PreExecutionProgramPath = ProcedureToExecuteBeforeAnyProgressExecutionFilePath,
                    ProExeCommandLineParameters = ExtraOpenedgeCommandLineParameters,
                    ProPathList = GetPropath((BuildOptions?.SourceDirectoryPath).TakeDefaultIfNeeded(OeBuildOptions.GetDefaultSourceDirectoryPath()), true).Select(d => d.Path).ToList()
                };
                if (!string.IsNullOrEmpty(OpenedgeCodePage)) {
                    _env.CodePageName = OpenedgeCodePage;
                }
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
                CompileInAnalysisMode = BuildOptions?.IncrementalBuildOptions?.Enabled ?? OeIncrementalBuildOptions.GetDefaultEnabled(),
                WorkingDirectory = workingDirectory,
                NeedDatabaseConnection = true,
                AnalysisModeSimplifiedDatabaseReferences = BuildOptions?.IncrementalBuildOptions?.UseSimplerAnalysisForDatabaseReference ?? OeIncrementalBuildOptions.GetDefaultUseSimplerAnalysisForDatabaseReference(),
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