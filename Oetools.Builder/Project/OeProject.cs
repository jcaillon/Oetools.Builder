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
using Oetools.Builder.Project.Task;
using Oetools.Builder.Resources;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// An openedge project (i.e. an application).
    /// </summary>
    /// <remarks>
    /// A project has:
    /// - properties, that are used to describe your application (for instance, the database needed to compile) and are also used to describe options to build your application (for instance, if the compilation should also generate the xref files).
    /// - variables, that make your build process dynamic. You can use variables almost anywhere in this xml and dynamically overload their values when running the build.
    /// - build configurations, which describe a succession of tasks that build your application. Build configurations can also have their own properties and variables which will overload the ones defined at the project level.
    /// </remarks>
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
            var serializer = new XmlSerializer(typeof(OeProject));
            
            XmlDocumentWriter.Save(path, serializer, this);

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
        
        /// <summary>
        /// The default properties for this project.
        /// </summary>
        /// <remarks>
        /// Properties can describe your application (for instance, the database needed to compile).
        /// Properties can also describe options to build your application (for instance, if the compilation should also generate the xref files).
        /// These properties are used as default values for this project but can be overloaded for each individual build configuration.
        /// For instance, this allows to define a DLC (v11) path for the project but you can define a build configuration that will use another DLC (v9) path.
        /// </remarks>
        [XmlElement("DefaultProperties")]
        public OeProperties DefaultProperties { get; set; }
        
        /// <summary>
        /// The global variables shared by all the build configurations.
        /// </summary>
        /// <remarks>
        /// Variables make your build process dynamic by allowing you to change build options without having to modify this xml.
        /// You can use a variable with the syntax {{variable_name}}.
        /// Variables will be replaced by their value at run time.
        /// If the variable exists as an environment variable, its value will be taken in priority (this allows to overload values using environment variables).
        /// Non existing variables will be replaced by an empty string.
        /// Variables can be used in any "string type" properties (this exclude numbers/booleans).
        /// You can use variables in the variables definition but they must be defined in the right order.
        ///
        /// Special variables are already defined and available:
        /// - {{SOURCE_DIRECTORY}} the application source directory (defined in properties)
        /// - {{PROJECT_DIRECTORY}} the project directory ({{SOURCE_DIRECTORY}}/.oe)
        /// - {{PROJECT_LOCAL_DIRECTORY}} the project local directory ({{SOURCE_DIRECTORY}}/.oe/local)
        /// - {{DLC}} the dlc path used for the current build
        /// - {{OUTPUT_DIRECTORY}} the build output directory (default to {{SOURCE_DIRECTORY}}/.oe/bin)
        /// - {{CONFIGURATION_NAME}} the build configuration name for the current build
        /// - {{CURRENT_DIRECTORY}} the current directory
        /// </remarks>
        [XmlArray("GlobalVariables")]
        [XmlArrayItem("Variable", typeof(OeVariable))]
        public List<OeVariable> GlobalVariables { get; set; }
        
        /// <summary>
        /// The build configurations list.
        /// </summary>
        /// <remarks>
        /// A build configuration describe how to build your application.
        /// It is essentially a succession of tasks (grouped into steps) that should be carried on in a sequential manner to build your application.
        /// You can have several build configurations for a single project.
        /// You can overload the project level properties and variables for each build configuration.
        /// </remarks>
        [XmlArray("BuildConfigurations")]
        [XmlArrayItem("Build", typeof(OeBuildConfiguration))]
        public List<OeBuildConfiguration> BuildConfigurations { get; set; }

        /// <summary>
        /// Returns an initialized project with some initialized properties.
        /// </summary>
        /// <returns></returns>
        public static OeProject GetStandardProject() {
            var output = new OeProject {
                DefaultProperties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        OutputDirectoryPath = OeBuilderConstants.GetDefaultOutputDirectory()
                    }
                },
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        BuildSourceStepGroup = new List<OeBuildStepBuildSource> {
                            new OeBuildStepBuildSource {
                                Name = "Compile all files next to their source",
                                Tasks = new List<AOeTask> {
                                    new OeTaskFileCompile {
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
            return GetBuildConfigurationCopy(BuildConfigurations?.FirstOrDefault() ?? new OeBuildConfiguration());
        }

        private OeBuildConfiguration GetBuildConfigurationCopy(OeBuildConfiguration buildConfiguration) {
            if (buildConfiguration == null) {
                return null;
            }
            
            // make a copy of the object since we want to modify it
            var output = buildConfiguration.GetDeepCopy();
            
            // we take the global properties by default but they can be overload by the build configuration properties
            output.Properties = DefaultProperties.GetDeepCopy();
            buildConfiguration.Properties.DeepCopy(output.Properties);
            
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