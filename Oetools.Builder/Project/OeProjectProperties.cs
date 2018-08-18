﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
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
        internal string GetDefaultDlcDirectoryPath() => ProUtilities.GetDlcPathFromEnv().ToCleanPath();
        
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
        internal bool GetDefaultAddAllSourceDirectoriesToPropath() => true;
            
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
        internal bool GetDefaultAddDefaultOpenedgePropath() => true;

        [XmlElement(ElementName = "UseCharacterModeExecutable")]
        public bool? UseCharacterModeExecutable { get; set; }
        internal bool GetDefaultUseCharacterModeExecutable() => false;

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
        internal string GetDefaultOutputDirectoryPath() => Path.Combine($"{{{{{OeBuilderConstants.OeVarNameSourceDirectory}}}}}", "bin");

        [XmlElement(ElementName = "ReportHtmlFilePath")]
        public string ReportHtmlFilePath { get; set; }
        internal string GetDefaultReportHtmlFilePath() => Path.Combine($"{{{{{OeBuilderConstants.OeProjectDirectory}}}}}", "build", "latest.html");
            
        [XmlElement(ElementName = "BuildHistoryOutputFilePath")]
        public string BuildHistoryOutputFilePath { get; set; }
        internal string GetDefaultBuildHistoryOutputFilePath() => Path.Combine($"{{{{{OeBuilderConstants.OeProjectDirectory}}}}}", "build", "latest.xml");
            
        [XmlElement(ElementName = "BuildHistoryInputFilePath")]
        public string BuildHistoryInputFilePath { get; set; }
        internal string GetDefaultBuildHistoryInputFilePath() => Path.Combine($"{{{{{OeBuilderConstants.OeProjectDirectory}}}}}", "build", "latest.xml");

        /// <summary>
        /// Validate that is object is correct
        /// </summary>
        public void Validate() {
            ValidateFilters(PropathFilters, nameof(PropathFilters));
            ValidateFilters(SourcePathFilters, nameof(SourcePathFilters));
        }
        
        private void ValidateFilters(IEnumerable<OeFilter> filters, string propertyNameOf) {
            var i = 0;
            foreach (var filter in filters) {
                try {
                    filter.Validate();
                } catch (Exception e) {
                    var et = e as FilterValidationException;
                    if (et != null) {
                        et.FilterNumber = i;
                        et.FilterCollectionName = typeof(OeProjectProperties).GetXmlName(propertyNameOf);
                    }
                    throw new BuildConfigurationException(et != null ? et.Message : "Unexpected exception when checking filters", et ?? e);
                }
                i++;
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
                foreach (var entry in ProUtilities.GetProPathFromIniFile(IniFilePath, sourceDirectory)) {
                    if (!output.Contains(entry)) {
                        output.Add(entry);
                    }
                }
            }
            if (AddAllSourceDirectoriesToPropath ?? false) {
                var lister = new SourceFilesLister(sourceDirectory) {
                    SourcePathFilters = PropathFilters
                };
                foreach (var directory in lister.GetDirectoryList()) {
                    if (!output.Contains(directory)) {
                        output.Add(directory);
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