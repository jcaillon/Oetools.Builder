using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
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
        public string DlcDirectoryPath { get; set; } = ProUtilities.GetDlcPathFromEnv();
        
        [XmlArray("ProjectDatabases")]
        [XmlArrayItem("ProjectDatabase", typeof(OeProjectDatabase))]
        public List<OeProjectDatabase> ProjectDatabases { get; set; }
            
        [Serializable]
        public class OeProjectDatabase {
      
            [XmlAttribute(AttributeName = "LogicalName")]
            [ReplaceVariables(SkipReplace = true)]
            public bool LogicalName { get; set; }
            
            [XmlAttribute(AttributeName = "DataDefinitionFilePath")]
            public string DataDefinitionFilePath { get; set; }
        
        }
        
        [XmlElement(ElementName = "DatabaseConnectionExtraParameters")]
        public string DatabaseConnectionExtraParameters { get; set; }

        [XmlArray("DatabaseAliases")]
        [XmlArrayItem("Alias", typeof(OeDatabaseAlias))]
        [ReplaceVariables(SkipReplace = true)]
        public List<OeDatabaseAlias> DatabaseAliases { get; set; }

        public class OeDatabaseAlias {
                
            [XmlAttribute(AttributeName = "ProgresCommandLineExtraParameters")]
            public string AliasLogicalName { get; set; }

            [XmlAttribute(AttributeName = "PreProgresExecutionProgramPath")]
            public string DatabaseLogicalName { get; set; }
        }
            
            
        [XmlElement(ElementName = "IniFilePath")]
        public string IniFilePath { get; set; }

        [XmlArrayItem("Path", typeof(string))]
        [XmlArray("PropathEntries")]
        public List<string> PropathEntries { get; set; }
        
        [XmlElement(ElementName = "AddAllSourceDirectoriesToPropath")]
        public bool AddAllSourceDirectoriesToPropath { get; set; }
            
        [XmlArray("PropathFilters")]
        [XmlArrayItem("Filter", typeof(OeFilter))]
        [XmlArrayItem("FilterRegex", typeof(OeFilterRegex))]
        public List<OeFilter> PropathFilters { get; set; }

        /// <summary>
        /// Adds the gui or tty (depending on <see cref="UseCharacterModeExecutable"/>) folder as well as the contained .pl to the propath
        /// Also adds dlc and dlc/bin
        /// </summary>
        [XmlElement(ElementName = "AddDefaultOpenedgePropath")]
        public bool AddDefaultOpenedgePropath { get; set; }

        [XmlElement(ElementName = "UseCharacterModeExecutable")]
        public bool UseCharacterModeExecutable { get; set; }

        [XmlElement(ElementName = "ProgresCommandLineExtraParameters")]
        public string ProgresCommandLineExtraParameters { get; set; }

        [XmlElement(ElementName = "ProcedurePathToExecuteBeforeAnyProgressExecution")]
        public string ProcedurePathToExecuteBeforeAnyProgressExecution { get; set; }

        [XmlElement(ElementName = "ProcedurePathToExecuteAfterAnyProgressExecution")]
        public string ProcedurePathToExecuteAfterAnyProgressExecution { get; set; }

        [XmlElement(ElementName = "TemporaryDirectoryPath")]
        public string TemporaryDirectoryPath { get; set; }
        
        /// <summary>
        /// Global variables applicable to all build
        /// TODO : useful for webclient variables like application name!
        /// </summary>
        [XmlArray("GlobalVariables")]
        [XmlArrayItem("Variable", typeof(OeVariable))]
        [ReplaceVariables(SkipReplace = true)]
        public List<OeVariable> GlobalVariables { get; set; }

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
            if (AddAllSourceDirectoriesToPropath) {
                List<string> propathExcludeRegexStrings = OeFilter.GetExclusionRegexStringsFromFilters(PropathFilters, sourceDirectory);
                foreach (var file in Utils.EnumerateAllFolders(sourceDirectory, SearchOption.AllDirectories, propathExcludeRegexStrings)) {
                    if (!output.Contains(file)) {
                        output.Add(file);
                    }
                }
            }
            if (AddDefaultOpenedgePropath) {
                // %DLC%/tty or %DLC%/gui + %DLC% + %DLC%/bin
                foreach (var file in ProUtilities.GetProgressSessionDefaultPropath(DlcDirectoryPath, UseCharacterModeExecutable)) {
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