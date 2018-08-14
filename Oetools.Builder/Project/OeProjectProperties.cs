using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeProjectProperties {

        /// <summary>
        /// Returns the propath that should be used considering all the options of this class
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="simplifyPathWithWorkingDirectory"></param>
        /// <returns></returns>
        public HashSet<string> GetPropath(string sourceDirectory, bool simplifyPathWithWorkingDirectory) {
            // read from ini
            var output = !string.IsNullOrEmpty(IniFilePath) ? ProUtilities.GetProPathFromIniFile(IniFilePath, sourceDirectory) : new HashSet<string>();
            List<string> propathExcludeRegexStrings = null;
            if (PropathFilters != null) {
                propathExcludeRegexStrings = PropathFilters
                    .SelectMany(f => f.Exclude.Split(';').Select(p => f is OePropathFilterRegex ? p : p.PathWildCardToRegex()))
                    .ToList();
            }
            if (AddAllSourceDirectoriesToPropath) {
                foreach (var file in Utils.EnumerateAllFiles(sourceDirectory, SearchOption.AllDirectories, propathExcludeRegexStrings)) {
                    if (!output.Contains(file)) {
                        output.Add(file);
                    }
                }
            }
            if (AddDefaultOpenedgePropath) {
                foreach (var file in ProUtilities.GetProgressSessionDefaultPropath(DlcDirectoryPath, UseCharacterModeExecutable)) {
                    if (!output.Contains(file)) {
                        output.Add(file);
                    }
                }
                if (!output.Contains(DlcDirectoryPath)) {
                    output.Add(DlcDirectoryPath);
                }
                if (!output.Contains(Path.Combine(DlcDirectoryPath, "bin"))) {
                    output.Add(Path.Combine(DlcDirectoryPath, "bin"));
                }
            }
            if (simplifyPathWithWorkingDirectory) {
                // TODO
            }
            return output;
        }

        [XmlArray("ProjectDatabases")]
        [XmlArrayItem("ProjectDatabase", typeof(OeProjectDatabase))]
        public List<OeProjectDatabase> ProjectDatabases { get; set; }
            
        [XmlElement(ElementName = "DatabaseConnectionExtraParameters")]
        public string DatabaseConnectionExtraParameters { get; set; }

        [XmlArray("DatabaseAliases")]
        [XmlArrayItem("Alias", typeof(OeDatabaseAlias))]
        public List<OeDatabaseAlias> DatabaseAliases { get; set; }

        public class OeDatabaseAlias {
                
            [XmlAttribute(AttributeName = "ProgresCommandLineExtraParameters")]
            public string AliasLogicalName { get; set; }

            [XmlAttribute(AttributeName = "PreProgresExecutionProgramPath")]
            public string DatabaseLogicalName { get; set; }
        }
            
            
        [XmlElement(ElementName = "IniFilePath")]
        public string IniFilePath { get; set; }

        [XmlElement(ElementName = "AddAllSourceDirectoriesToPropath")]
        public bool AddAllSourceDirectoriesToPropath { get; set; }
        
        /// <summary>
        /// Adds the gui or tty (depending on <see cref="UseCharacterModeExecutable"/>) folder as well as the contained .pl to the propath
        /// Also adds dlc and dlc/bin
        /// </summary>
        [XmlElement(ElementName = "AddDefaultOpenedgePropath")]
        public bool AddDefaultOpenedgePropath { get; set; }

        [XmlArrayItem("Path", typeof(string))]
        [XmlArray("PropathEntries")]
        public List<string> PropathEntries { get; set; }
            
        [XmlArray("PropathFilters")]
        [XmlArrayItem("Filter", typeof(OePropathFilter))]
        [XmlArrayItem("FilterRegex", typeof(OePropathFilterRegex))]
        public List<OePropathFilter> PropathFilters { get; set; }

        public class OePropathFilter { 
            [XmlAttribute(AttributeName = "Exclude")]
            public string Exclude { get; set; }
        }

        public class OePropathFilterRegex : OePropathFilter { }

        [XmlElement(ElementName = "DlcDirectoryPath")]
        public string DlcDirectoryPath { get; set; } = ProUtilities.GetDlcPathFromEnv();

        [XmlElement(ElementName = "UseCharacterModeExecutable")]
        public bool UseCharacterModeExecutable { get; set; }

        [XmlElement(ElementName = "ProgresCommandLineExtraParameters")]
        public string ProgresCommandLineExtraParameters { get; set; }

        [XmlElement(ElementName = "ProcedurePathToExecuteBeforeAnyProgressExecution")]
        public string ProcedurePathToExecuteBeforeAnyProgressExecution { get; set; }

        [XmlElement(ElementName = "ProcedurePathToExecuteAfterAnyProgressExecution")]
        public string ProcedurePathToExecuteAfterAnyProgressExecution { get; set; }

        [XmlElement(ElementName = "TemporaryDirectory")]
        public string TemporaryDirectory { get; set; }
    }
    
    [Serializable]
    public class OeProjectDatabase {
      
        [XmlAttribute(AttributeName = "LogicalName")]
        public bool LogicalName { get; set; }
            
        [XmlAttribute(AttributeName = "DataDefinitionFilePath")]
        public string DataDefinitionFilePath { get; set; }
        
    }
}