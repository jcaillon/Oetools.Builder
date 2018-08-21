#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeProject.cs) is part of Oetools.Builder.
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
using System.Xml.Schema;
using System.Xml.Serialization;
using Oetools.Builder.Resources;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Project {
    
    [Serializable]
    [XmlRoot("Project")]
    public class OeProject {
        
        #region static

        private const string XsdName = "Project.xsd";

        public static OeProject Load(string path) {
            OeProject interfaceXml;
            var serializer = new XmlSerializer(typeof(OeProject));
            using (var reader = new StreamReader(path)) {
                interfaceXml = (OeProject) serializer.Deserialize(reader);
            }
            interfaceXml.InitIds();
            return interfaceXml;
        }

        public void Save(string path) {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("xsi", XmlSchema.InstanceNamespace);
            var serializer = new XmlSerializer(typeof(OeProject));

            using (TextWriter writer = new StreamWriter(path, false)) {
                serializer.Serialize(writer, this, namespaces);
            }
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path) ?? "", XsdName), XsdResources.GetXsdFromResources(XsdName));
        }

        #endregion
        
#if USESCHEMALOCATION
        /// <summary>
        /// Only when not generating the build for xsd.exe which has a problem with this attribute
        /// </summary>
        [XmlAttribute("noNamespaceSchemaLocation", Namespace = XmlSchema.InstanceNamespace)]
        public string SchemaLocation = XsdName;
#endif
        
        [XmlElement("Properties")]
        public OeProjectProperties GlobalProperties { get; set; }
        
        /// <summary>
        /// Global variables applicable to all build
        /// TODO : useful for webclient variables like application name!
        /// </summary>
        [XmlArray("GlobalVariables")]
        [XmlArrayItem("Variable", typeof(OeVariable))]
        public List<OeVariable> GlobalVariables { get; set; }
        
        [XmlArray("BuildConfigurations")]
        [XmlArrayItem("Build", typeof(OeBuildConfiguration))]
        public List<OeBuildConfiguration> BuildConfigurations { get; set; }

        /// <summary>
        /// Returns an initialized project with default properties
        /// </summary>
        /// <returns></returns>
        public static OeProject GetDefaultProject() {
            var output = new OeProject {
                GlobalProperties = new OeProjectProperties {
                    OutputDirectoryPath = Path.Combine("{{SOURCE_DIRECTORY}}", "bin")
                },
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        BuildSourceTasks = new List<OeBuildStepCompile> {
                            new OeBuildStepCompile {
                                Label = "Compile all files next to their source",
                                Tasks = new List<OeTask> {
                                    new OeTaskFileTargetFileCompile {
                                        Include = "((**))*",
                                        TargetDirectory = "{{1}}"
                                    }
                                }
                            }
                        }
                    }
                }
            };
            return output;
        }

        /// <summary>
        /// Returns a copy of the build configuration with the given name, or null by default
        /// </summary>
        /// <param name="configurationName"></param>
        /// <returns></returns>
        public OeBuildConfiguration GetBuildConfigurationCopy(string configurationName) {
            if (configurationName == null) {
                return null;
            }
            return GetBuildConfigurationCopy(BuildConfigurations.FirstOrDefault(bc => bc.ConfigurationName.Equals(configurationName, StringComparison.CurrentCultureIgnoreCase)));
        }

        /// <summary>
        /// Returns the first build configuration found, or null
        /// </summary>
        /// <returns></returns>
        public OeBuildConfiguration GetDefaultBuildConfigurationCopy() {
            return GetBuildConfigurationCopy(BuildConfigurations.FirstOrDefault());
        }

        private OeBuildConfiguration GetBuildConfigurationCopy(OeBuildConfiguration buildConfiguration) {
            if (buildConfiguration == null) {
                return null;
            }
            
            // make a copy of the object since we want to modify it
            var output = (OeBuildConfiguration) Utils.DeepCopyPublicProperties(buildConfiguration, typeof(OeBuildConfiguration));
            
            // we take the global properties by default but they can be overload by the build configuration properties
            output.Properties = (OeProjectProperties) Utils.DeepCopyPublicProperties(buildConfiguration.Properties, typeof(OeProjectProperties), GlobalProperties);
            
            // add global variables to the build configuration
            if (GlobalVariables != null) {
                foreach (var globalVariable in GlobalVariables) {
                    (output.Variables ?? (output.Variables = new List<OeVariable>())).Add(globalVariable);
                }
            }
            return output;
        }

        /// <summary>
        /// Give each configuration/variables a unique number to identify it
        /// </summary>
        internal void InitIds() {
            if (BuildConfigurations != null) {
                var i = 0;
                foreach (var configuration in BuildConfigurations.Where(s => s != null)) {
                    configuration.InitIds();
                    configuration.Id = i;
                    i++;
                }
            }
            if (GlobalVariables != null) {
                var i = 0;
                foreach (var variable in GlobalVariables.Where(v => v != null)) {
                    variable.Id = i;
                    i++;
                }
            }
        }
    }
    
}