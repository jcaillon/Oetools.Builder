#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeProjectProperties.cs) is part of Oetools.Builder.
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
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project {
    
    /// <remarks>
    /// Every public property string not marked with the <see cref="ReplaceVariables"/> attribute is allowed
    /// to use &lt;VARIABLE&gt; which will be replace at the beggining of the build by <see cref="OeBuildConfiguration.Variables"/>
    /// </remarks>
    [Serializable]
    public class OeProjectProperties {
        
        [XmlElement(ElementName = "DlcDirectoryPath")]
        public string DlcDirectoryPath { get; set; }
        internal static string GetDefaultDlcDirectoryPath() => UoeUtilities.GetDlcPathFromEnv().ToCleanPath();
        
        [XmlArray("ProjectDatabases")]
        [XmlArrayItem("ProjectDatabase", typeof(OeProjectDatabase))]
        public List<OeProjectDatabase> ProjectDatabases { get; set; }
        
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
        internal static bool GetDefaultAddAllSourceDirectoriesToPropath() => true;
            
        [XmlElement(ElementName = "PropathSourceDirectoriesFilter")]
        public OeTaskFilter PropathSourceDirectoriesFilter { get; set; }

        /// <summary>
        /// Adds the gui or tty (depending on <see cref="UseCharacterModeExecutable"/>) folder as well as the contained .pl to the propath
        /// Also adds dlc and dlc/bin
        /// </summary>
        [XmlElement(ElementName = "AddDefaultOpenedgePropath")]
        public bool? AddDefaultOpenedgePropath { get; set; }
        internal static bool GetDefaultAddDefaultOpenedgePropath() => true;

        [XmlElement(ElementName = "UseCharacterModeExecutable")]
        public bool? UseCharacterModeExecutable { get; set; }
        internal static bool GetDefaultUseCharacterModeExecutable() => false;

        [XmlElement(ElementName = "ProgresCommandLineExtraParameters")]
        public string ProgresCommandLineExtraParameters { get; set; }

        [XmlElement(ElementName = "ProcedurePathToExecuteBeforeAnyProgressExecution")]
        public string ProcedurePathToExecuteBeforeAnyProgressExecution { get; set; }

        [XmlElement(ElementName = "ProcedurePathToExecuteAfterAnyProgressExecution")]
        public string ProcedurePathToExecuteAfterAnyProgressExecution { get; set; }

        [XmlElement(ElementName = "TemporaryDirectoryPath")]
        public string TemporaryDirectoryPath { get; set; }      
        
        /// <summary>
        /// Allows to exclude path from being treated by <see cref="OeBuildConfiguration.BuildSourceTasks"/>
        /// Specify what should not be considered as a source file in your source directory (for instance, the docs/ folder)
        /// </summary>
        [XmlElement(ElementName = "SourceToBuildPathFilter")]
        public OeTaskFilter SourceToBuildPathFilter { get; set; }
                
        /// <summary>
        /// Use this to apply GIT filters to your <see cref="OeBuildConfiguration.BuildSourceTasks"/>
        /// Obviously, you need GIT installed and present in your OS path
        /// </summary>
        [XmlElement(ElementName = "SourceToBuildGitFilter")]
        public OeGitFilter SourceToBuildGitFilter { get; set; }       
                  
        [XmlElement(ElementName = "CompilationOptions")]
        public OeCompilationOptions CompilationOptions { get; set; }
            
        [XmlElement(ElementName = "IncrementalBuildOptions")]
        public OeIncrementalBuildOptions IncrementalBuildOptions { get; set; }

        [XmlElement(ElementName = "OutputDirectoryPath")]
        public string OutputDirectoryPath { get; set; }
        internal static string GetDefaultOutputDirectoryPath(string sourceDirectory) => Path.Combine(sourceDirectory, "bin");

        [XmlElement(ElementName = "ReportHtmlFilePath")]
        public string ReportHtmlFilePath { get; set; }
        internal static string GetDefaultReportHtmlFilePath(string sourceDirectory) => Path.Combine(sourceDirectory, OeBuilderConstants.OeProjectDirectory, "build", "latest.html");
            
        [XmlElement(ElementName = "BuildHistoryOutputFilePath")]
        public string BuildHistoryOutputFilePath { get; set; }
        internal static string GetDefaultBuildHistoryOutputFilePath(string sourceDirectory) => Path.Combine(sourceDirectory, OeBuilderConstants.OeProjectDirectory, "build", "latest.xml");
            
        [XmlElement(ElementName = "BuildHistoryInputFilePath")]
        public string BuildHistoryInputFilePath { get; set; }
        internal static string GetDefaultBuildHistoryInputFilePath(string sourceDirectory) => Path.Combine(sourceDirectory, OeBuilderConstants.OeProjectDirectory, "build", "latest.xml");

        /// <summary>
        /// Validate that is object is correct
        /// </summary>
        /// <exception cref="FilterValidationException"></exception>
        public void Validate() {
            ValidateFilters(PropathSourceDirectoriesFilter, nameof(PropathSourceDirectoriesFilter));
            ValidateFilters(SourceToBuildPathFilter, nameof(SourceToBuildPathFilter));
        }
        
        private void ValidateFilters(OeTaskFilter filter, string propertyNameOf) {
            try {
                filter.Validate();
            } catch (Exception e) {
                var et = e as FilterValidationException;
                if (et != null) {
                    et.FilterCollectionName = typeof(OeProjectProperties).GetXmlName(propertyNameOf);
                }
                throw new BuildConfigurationException(et != null ? et.Message : "Unexpected exception when checking filters", et ?? e);
            }
        }

        /// <summary>
        /// Clean the path of all path properties
        /// </summary>
        public void SanitizePathInPublicProperties() {
            Utils.ForEachPublicPropertyStringInObject(typeof(OeProjectProperties), this, (propInfo, value) => {
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
        public List<string> GetPropath(string sourceDirectory, bool simplifyPathWithWorkingDirectory) {
            var output = new HashSet<string>();
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
                if (!output.Contains(entry)) {
                    output.Add(entry);
                }
            }
            // read from ini
            if (!string.IsNullOrEmpty(IniFilePath)) {
                foreach (var entry in UoeUtilities.GetProPathFromIniFile(IniFilePath, sourceDirectory)) {
                    if (!output.Contains(entry)) {
                        output.Add(entry);
                    }
                }
            }
            if (AddAllSourceDirectoriesToPropath ?? GetDefaultAddAllSourceDirectoriesToPropath()) {
                var lister = new SourceFilesLister(sourceDirectory) {
                    SourcePathFilter = PropathSourceDirectoriesFilter
                };
                foreach (var directory in lister.GetDirectoryList()) {
                    if (!output.Contains(directory)) {
                        output.Add(directory);
                    }
                }
            }
            if (AddDefaultOpenedgePropath ?? GetDefaultAddDefaultOpenedgePropath()) {
                // %DLC%/tty or %DLC%/gui + %DLC% + %DLC%/bin
                foreach (var file in UoeUtilities.GetProgressSessionDefaultPropath(DlcDirectoryPath, UseCharacterModeExecutable ?? GetDefaultUseCharacterModeExecutable())) {
                    if (!output.Contains(file)) {
                        output.Add(file);
                    }
                }
            }
            if (simplifyPathWithWorkingDirectory) {
                return output.ToList().Select(s => s.FromAbsolutePathToRelativePath(sourceDirectory)).ToList();
            }
            return output.ToList();
        }

        public UoeExecutionEnv GetOeExecutionEnvironment(string sourceDirectory) => 
            new UoeExecutionEnv {
                TempDirectory = TemporaryDirectoryPath?.TakeDefaultIfNeeded($".oe_tmp-{Utils.GetRandomName()}"),
                UseProgressCharacterMode = UseCharacterModeExecutable ?? GetDefaultUseCharacterModeExecutable(),
                DatabaseAliases = DatabaseAliases,
                DatabaseConnectionString = DatabaseConnectionExtraParameters,
                DatabaseConnectionStringAppendMaxTryOne = true,
                DlcDirectoryPath = DlcDirectoryPath.TakeDefaultIfNeeded(GetDefaultDlcDirectoryPath()),
                IniFilePath = IniFilePath,
                PostExecutionProgramPath = ProcedurePathToExecuteAfterAnyProgressExecution,
                PreExecutionProgramPath = ProcedurePathToExecuteBeforeAnyProgressExecution,
                ProExeCommandLineParameters = ProgresCommandLineExtraParameters,
                ProPathList = GetPropath(sourceDirectory, true)
            };

        public UoeExecutionParallelCompile GetPar(IUoeExecutionEnv env, string workingDirectory) =>
            new UoeExecutionParallelCompile(env) {
                AnalysisModeSimplifiedDatabaseReferences = CompilationOptions?.UseSimplerAnalysisForDatabaseReference ?? OeCompilationOptions.GetDefaultUseSimplerAnalysisForDatabaseReference(),
                CompileInAnalysisMode = IncrementalBuildOptions?.Disabled ?? OeIncrementalBuildOptions.GetDefaultDisabled(),
                CompileOptions = CompilationOptions?.CompileOptions,
                CompilerMultiCompile = CompilationOptions?.UseCompilerMultiCompile ?? OeCompilationOptions.GetDefaultUseCompilerMultiCompile(),
                CompileStatementExtraOptions = CompilationOptions?.CompileStatementExtraOptions,
                CompileUseXmlXref = CompilationOptions?.CompileWithXmlXref ?? OeCompilationOptions.GetDefaultCompileWithXmlXref(),
                CompileWithDebugList = CompilationOptions?.CompileWithDebugList ?? OeCompilationOptions.GetDefaultCompileWithDebugList(),
                CompileWithListing = CompilationOptions?.CompileWithListing ?? OeCompilationOptions.GetDefaultCompileWithListing(),
                CompileWithPreprocess = CompilationOptions?.CompileWithPreprocess ?? OeCompilationOptions.GetDefaultCompileWithPreprocess(),
                CompileWithXref = CompilationOptions?.CompileWithXref ?? OeCompilationOptions.GetDefaultCompileWithXref(),
                MaxNumberOfProcesses = Math.Max(1, Environment.ProcessorCount * CompilationOptions?.CompileNumberProcessPerCore ?? OeCompilationOptions.GetDefaultCompileNumberProcessPerCore()),
                MinimumNumberOfFilesPerProcess = CompilationOptions?.CompileMinimumNumberOfFilesPerProcess ?? OeCompilationOptions.GetDefaultCompileMinimumNumberOfFilesPerProcess(),
                WorkingDirectory = workingDirectory,
                NeedDatabaseConnection = true
            };
    }
}