﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge;

namespace Oetools.Builder.Project {
    
    /// <remarks>
    /// Every public property string not marked with the <see cref="ReplaceVariables"/> attribute is allowed
    /// to use &lt;VARIABLE&gt; which will be replace at the beggining of the build by <see cref="OeBuildConfiguration.Variables"/>
    /// </remarks>
    [Serializable]
    public class OeProjectProperties {
        
        [XmlElement(ElementName = "DlcDirectoryPath")]
        public string DlcDirectoryPath { get; set; }
        
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
            
        [XmlArray("PropathFilters")]
        [XmlArrayItem("Filter", typeof(OeFilter))]
        [XmlArrayItem("FilterRegex", typeof(OeFilterRegex))]
        public List<OeFilter> PropathFilters { get; set; }

        /// <summary>
        /// Adds the gui or tty (depending on <see cref="UseCharacterModeExecutable"/>) folder as well as the contained .pl to the propath
        /// Also adds dlc and dlc/bin
        /// </summary>
        [XmlElement(ElementName = "AddDefaultOpenedgePropath")]
        public bool? AddDefaultOpenedgePropath { get; set; }

        [XmlElement(ElementName = "UseCharacterModeExecutable")]
        public bool? UseCharacterModeExecutable { get; set; }

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
        [XmlArray("SourcePathFilters")]
        [XmlArrayItem("Filter", typeof(OeFilter))]
        [XmlArrayItem("FilterRegex", typeof(OeFilterRegex))]
        public List<OeFilter> SourcePathFilters { get; set; }
                
        /// <summary>
        /// Use this to apply GIT filters to your <see cref="OeBuildConfiguration.BuildSourceTasks"/>
        /// Obviously, you need GIT installed and present in your OS path
        /// </summary>
        [XmlElement(ElementName = "SourcePathGitFilter")]
        public OeGitFilter SourcePathGitFilter { get; set; }       
                  
        [XmlElement(ElementName = "CompilationOptions")]
        public OeCompilationOptions CompilationOptions { get; set; }
            
        [XmlElement(ElementName = "IncrementalBuildOptions")]
        public OeIncrementalBuildOptions IncrementalBuildOptions { get; set; }

        [XmlElement(ElementName = "OutputDirectoryPath")]
        public string OutputDirectoryPath { get; set; }

        [XmlElement(ElementName = "ReportFilePath")]
        public string ReportFilePath { get; set; }
            
        [XmlElement(ElementName = "BuildHistoryOutputFilePath")]
        public string BuildHistoryOutputFilePath { get; set; }
            
        [XmlElement(ElementName = "BuildHistoryInputFilePath")]
        public string BuildHistoryInputFilePath { get; set; }

        /// <summary>
        /// Validate that is object is correct
        /// </summary>
        public void Validate() { }

        /// <summary>
        /// Set default values for certain properties if they are null
        /// </summary>
        public void SetDefaultValuesWhenNeeded() {
            DlcDirectoryPath = DlcDirectoryPath ?? ProUtilities.GetDlcPathFromEnv();
            OutputDirectoryPath = OutputDirectoryPath ?? Path.Combine("<SOURCE_DIRECTORY>", "bin");
            ReportFilePath = ReportFilePath ?? Path.Combine("<PROJECT_DIRECTORY>", "build", "latest.html");
            BuildHistoryInputFilePath = BuildHistoryInputFilePath ?? Path.Combine("<PROJECT_DIRECTORY>", "build", "latest.xml");
            BuildHistoryOutputFilePath = BuildHistoryOutputFilePath ?? Path.Combine("<PROJECT_DIRECTORY>", "build", "latest.xml");
            AddAllSourceDirectoriesToPropath = AddAllSourceDirectoriesToPropath ?? true;
            AddDefaultOpenedgePropath = AddDefaultOpenedgePropath ?? true;
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
                foreach (var entry in ProUtilities.GetProPathFromIniFile(IniFilePath, sourceDirectory)) {
                    if (!output.Contains(entry)) {
                        output.Add(entry);
                    }
                }
            }
            if (AddAllSourceDirectoriesToPropath ?? false) {
                List<string> propathExcludeRegexStrings = OeFilter.GetExclusionRegexStringsFromFilters(PropathFilters, sourceDirectory);
                foreach (var file in Utils.EnumerateAllFolders(sourceDirectory, SearchOption.AllDirectories, propathExcludeRegexStrings)) {
                    if (!output.Contains(file)) {
                        output.Add(file);
                    }
                }
            }
            if (AddDefaultOpenedgePropath ?? false) {
                // %DLC%/tty or %DLC%/gui + %DLC% + %DLC%/bin
                foreach (var file in ProUtilities.GetProgressSessionDefaultPropath(DlcDirectoryPath, UseCharacterModeExecutable ?? false)) {
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
    }
}