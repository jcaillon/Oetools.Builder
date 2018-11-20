#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeBuildConfiguration.cs) is part of Oetools.Builder.
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
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Attributes;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// Represents the configuration of a build
    /// </summary>
    /// <inheritdoc cref="OeProject.BuildConfigurations"/>
    /// <code>
    /// Every public property string not marked with the <see cref="ReplaceVariables"/> attribute is allowed
    /// to use {{VARIABLE}} which will be replace at the beginning of the build by <see cref="Variables"/>.
    /// </code>
    [Serializable]
    [XmlRoot("BuildConfiguration")]
    public class OeBuildConfiguration {
            
        [XmlIgnore]
        internal int Id { get; set; }
        
        /// <summary>
        /// The name of this configuration. Purely informative.
        /// </summary>
        [XmlAttribute("Name")]
        public string Name { get; set; }
            
        /// <summary>
        /// The variables of this build configurations.
        /// </summary>
        /// <remarks>
        /// Variables make your build process dynamic by allowing you to change build options without having to modify this xml.
        /// You can use a variable with the syntax {{variable_name}}.
        /// Variables will be replaced by their value at run time.
        /// If the variable exists as an environment variable, its value will be taken in priority (this allows to overload values using environment variables).
        /// Non existing variables will be replaced by an empty string.
        /// Variables can be used in any "string type" properties (this exclude numbers/booleans).
        /// You can use variables in the variables definition but they must be defined in the right order.
        /// If several variables with the same name exist, the value of the latest defined is used.
        /// Variable names are case insensitive. 
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
        [XmlArray("Variables")]
        [XmlArrayItem("Variable", typeof(OeVariable))]
        [ReplaceVariables(SkipReplace = true)]
        public List<OeVariable> Variables { get; set; }
        
        /// <summary>
        /// The properties of this build configuration.
        /// </summary>
        /// <remarks>
        /// Properties can describe your application (for instance, the database needed to compile).
        /// Properties can also describe options to build your application (for instance, if the compilation should also generate the xref files).
        /// These properties are used as default values for this project but can be overloaded for each individual build configuration.
        /// For instance, this allows to define a DLC (v11) path for the project but you can define a build configuration that will use another DLC (v9) path.
        /// </remarks>
        [XmlElement("Properties")]
        public OeProperties Properties { get; set; }
        public static OeProperties GetDefaultProperties() => new OeProperties();
        
        /// <summary>
        /// A list of steps/tasks that will be executed before anything else.
        /// </summary>
        /// <remarks>
        /// These tasks can be used to "prepare" the build. For instance, by downloading dependencies or packages. Or by modifying certain source files.
        /// Steps and tasks within steps are executed sequentially in the given order.
        /// </remarks>
        [XmlArray("PreBuildTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> PreBuildStepGroup { get; set; }

        /// <summary>
        /// A list of steps/tasks that will build the files in your project source directory.
        /// </summary>
        /// <remarks>
        /// This is the main tasks list, where openedge files should be compiled.
        /// The history of files built here can be saved to enable an incremental build.
        /// A listing of the source files is made at each step. Which means it would not be efficient to create 10 steps of 1 task each if the files in your source directory will not change between steps.
        /// Steps and tasks within steps are executed sequentially in the given order.
        /// </remarks>
        [XmlArray("BuildSourceTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepBuildSource))]
        public List<OeBuildStepBuildSource> BuildSourceStepGroup { get; set; }
            
        /// <summary>
        /// A list of steps/tasks that should affect the files in your project output directory.
        /// </summary>
        /// <remarks>
        /// These tasks should be used to "post-process" the files built from your source directory into the output directory.
        /// For instance, it can be used to build a release zip file containing all the .pl and other configuration files of your release.
        /// A listing of the files in the output directory is made at each step. Which means it would not be efficient to create 10 steps of 1 task each if those files will not change between steps.
        /// Steps and tasks within steps are executed sequentially in the given order.
        /// </remarks>
        [XmlArray("BuildOutputTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> BuildOutputStepGroup { get; set; }
        
        /// <summary>
        /// A list of steps/tasks that will be executed after anything else.
        /// </summary>
        /// <remarks>
        /// These tasks can be used to "deploy" a build. For instance, by uploading a release zip file to a distant http or ftp server.
        /// Steps and tasks within steps are executed sequentially in the given order.
        /// </remarks>
        [XmlArray("PostBuildTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> PostBuildStepGroup { get; set; }
        
        /// <summary>
        /// A list of children build configurations, each will inherit the properties defined in this one.
        /// </summary>
        /// <inheritdoc cref="OeProject.BuildConfigurations"/>
        [XmlArray("ChildrenBuildConfigurations")]
        [XmlArrayItem("Configuration", typeof(OeBuildConfiguration))]
        [DeepCopy(Ignore = true)]
        public List<OeBuildConfiguration> BuildConfigurations { get; set; }
        
        /// <summary>
        /// Sets default values to all the properties (and recursively) of this object, using the GetDefault[Property] methods.
        /// Only replaces non null values.
        /// </summary>
        public void SetDefaultValues() {
            Utils.SetDefaultValues(this);
        }
                    
        /// <summary>
        /// Add the default variables and apply the variables on all public properties of type string
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="BuildVariableException"></exception>
        /// <exception cref="BuildConfigurationException"></exception>
        public void ApplyVariables() {
            Variables = Variables ?? new List<OeVariable>();          
            
            string currentDirectory;
            try {
                currentDirectory = Directory.GetCurrentDirectory();
            } catch (Exception e) {
                throw new BuildConfigurationException(this, "Failed to get the current directory (check permissions).", e);
            }
            currentDirectory = currentDirectory.ToCleanPath();
            var sourceDirectory = (Properties?.BuildOptions?.SourceDirectoryPath).TakeDefaultIfNeeded(OeBuildOptions.GetDefaultSourceDirectoryPath());

            var originalVariablesList = Variables;
            
            // add some default variables
            Variables = new List<OeVariable> {
                new OeVariable {
                    Name = OeBuilderConstants.OeVarNameCurrentDirectory, Value = currentDirectory
                },
                new OeVariable {
                    Name = OeBuilderConstants.OeVarNameSourceDirectory, Value = sourceDirectory
                },
                new OeVariable {
                    Name = OeBuilderConstants.OeVarNameProjectDirectory, Value = OeBuilderConstants.GetProjectDirectory(sourceDirectory)
                },
                new OeVariable {
                    Name = OeBuilderConstants.OeVarNameProjectLocalDirectory, Value = OeBuilderConstants.GetProjectDirectoryLocal(sourceDirectory)
                },
                new OeVariable {
                    Name = UoeConstants.OeDlcEnvVar, Value = (Properties?.DlcDirectory).TakeDefaultIfNeeded(OeProperties.GetDefaultDlcDirectory())
                },
                new OeVariable {
                    Name = OeBuilderConstants.OeVarNameOutputDirectory, Value = (Properties?.BuildOptions?.OutputDirectoryPath).TakeDefaultIfNeeded(OeBuilderConstants.GetDefaultOutputDirectory())
                },
                new OeVariable {
                    Name = OeBuilderConstants.OeVarNameConfigurationName, Value = Name
                }
            };
            // extra variable FILE_SOURCE_DIRECTORY defined only when computing targets
            
            // add the original list back
            Variables.AddRange(originalVariablesList);
            
            // apply variables on variables
            BuilderUtilities.ApplyVariablesInVariables(Variables);

            // apply variables in all public string properties (reverse to apply the last defined variables first)
            Variables.Reverse();
            BuilderUtilities.ApplyVariablesToProperties(this, Variables);  
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="BuildConfigurationException"></exception>
        public void Validate() {
            try {
                ValidateAllTasks();
                Properties?.Validate();
            } catch (Exception e) {
                throw new BuildConfigurationException(this, e.Message, e);
            }
        }
            
        /// <summary>
        /// Recursively validates that the build configuration is correct
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="BuildStepException"></exception>
        public void ValidateAllTasks() {
            ValidateStepsList(PreBuildStepGroup, nameof(PreBuildStepGroup), false);
            ValidateStepsList(BuildSourceStepGroup, nameof(BuildSourceStepGroup), true);
            ValidateStepsList(BuildOutputStepGroup, nameof(BuildOutputStepGroup), true);
            ValidateStepsList(PostBuildStepGroup, nameof(PostBuildStepGroup), false);
        }
        
        private void ValidateStepsList(IEnumerable<AOeBuildStep> steps, string propertyNameOf, bool buildFromList) {
            if (steps == null) {
                return;
            }
            foreach (var step in steps) {
                try {
                    step.Validate(buildFromList);
                } catch (BuildStepException e) {
                    e.PropertyName = typeof(OeBuildConfiguration).GetXmlName(propertyNameOf);
                    throw;
                }
            }
        }
        
        public override string ToString() => $"Configuration [{Id}]{(string.IsNullOrEmpty(Name) ? "" : $" {Name}")}";

    }

}