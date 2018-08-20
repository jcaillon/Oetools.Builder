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
using System.Xml.Schema;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Resources;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Attributes;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// Represents the configuration of a build
    /// </summary>
    /// <remarks>
    /// Every public property string not marked with the <see cref="ReplaceVariables"/> attribute is allowed
    /// to use &lt;VARIABLE&gt; which will be replace at the beggining of the build by <see cref="Variables"/>
    /// </remarks>
    [Serializable]
    [XmlRoot("BuildConfiguration")]
    public class OeBuildConfiguration {
                
        #region static

        private const string XsdName = "BuildConfiguration.xsd";

        public static OeBuildConfiguration Load(string path) {
            OeBuildConfiguration interfaceXml;
            var serializer = new XmlSerializer(typeof(OeBuildConfiguration));
            using (var reader = new StreamReader(path)) {
                interfaceXml = (OeBuildConfiguration) serializer.Deserialize(reader);
            }

            return interfaceXml;
        }

        public void Save(string path) {
#if USESCHEMALOCATION
            SchemaLocation = XsdName;
#endif
            var serializer = new XmlSerializer(typeof(OeBuildConfiguration));
            using (TextWriter writer = new StreamWriter(path, false)) {
                serializer.Serialize(writer, this);
            }
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path) ?? "", XsdName), XsdResources.GetXsdFromResources(XsdName));
        }

        #endregion

#if USESCHEMALOCATION
        /// <summary>
        /// Only when not generating the build for xsd.exe which has a problem with this attribute
        /// </summary>
        [XmlAttribute("noNamespaceSchemaLocation", Namespace = XmlSchema.InstanceNamespace)]
        public string SchemaLocation { get; set; }
#endif
            
        [XmlAttribute("Name")]
        public string ConfigurationName { get; set; }
        
        /// <summary>
        /// Every existing sub node (even if empty) present in this will replace their <see cref="OeProject.GlobalProperties"/>
        /// counterpart
        /// </summary>
        [XmlElement("OverloadProperties")]
        [DeepCopy(Ignore = true)]
        public OeProjectProperties Properties { get; set; }
            
        /// <summary>
        /// Default variables are added by the builder, see <see cref="ApplyVariables"/>, also <see cref="OeProject.GlobalProperties"/>
        /// are always added
        /// </summary>
        [XmlArray("BuildVariables")]
        [XmlArrayItem("Variable", typeof(OeVariable))]
        [ReplaceVariables(SkipReplace = true)]
        public List<OeVariable> Variables { get; set; }
        
        /// <summary>
        /// This list of tasks can include any file
        /// </summary>
        [XmlArray("PreBuildTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> PreBuildTasks { get; set; }

        /// <summary>
        /// This list of tasks can only include files located in the source directory
        /// </summary>
        [XmlArray("BuildSourceTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepCompile))]
        public List<OeBuildStepCompile> BuildSourceTasks { get; set; }
            
        /// <summary>
        /// This list of tasks can only include files located in the output directory
        /// </summary>
        [XmlArray("BuildOutputTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> BuildOutputTasks { get; set; }
        
        /// <summary>
        /// This list of tasks can include any file
        /// </summary>
        [XmlArray("PostBuildTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> PostBuildTasks { get; set; }
                    
        /// <summary>
        /// Add the default variables and apply the variables on all public properties of type string
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="BuildVariableException"></exception>
        /// <exception cref="BuildConfigurationException"></exception>
        public void ApplyVariables(string sourceDirectory) {
            // add some default variables
            if (!string.IsNullOrEmpty(sourceDirectory)) {
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameSourceDirectory, Value = sourceDirectory });    
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameProjectDirectory, Value = Path.Combine(sourceDirectory, OeBuilderConstants.OeProjectDirectory) });                
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameProjectLocalDirectory, Value = Path.Combine(sourceDirectory, OeBuilderConstants.OeProjectDirectory, OeBuilderConstants.OeProjectLocalDirectory) });                 
            }             
            Variables.Add(new OeVariable { Name = UoeConstants.OeDlcEnvVar, Value = Properties.DlcDirectoryPath });  
            Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameOutputDirectory, Value = Properties.OutputDirectoryPath });  
            Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameConfigurationName, Value = ConfigurationName });
            try {
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameWorkingDirectory, Value = Directory.GetCurrentDirectory() });
            } catch (Exception e) {
                throw new BuildConfigurationException("Failed to get the current directory (check permissions)", e);
            }
            // extra variable FILE_SOURCE_DIRECTORY defined only when computing targets
            
            // apply variables on variables
            BuilderUtilities.ApplyVariablesInVariables(Variables);
            
            // apply variables in all public string properties
            BuilderUtilities.ApplyVariablesToProperties(this, Variables);  
        }

        public void Validate() {
            ValidateAllTasks();
            Properties?.Validate();
        }
            
        /// <summary>
        /// Recursively validates that the build configuration is correct
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="BuildConfigurationException"></exception>
        public void ValidateAllTasks() {
            ValidateStepsList(PreBuildTasks, nameof(PreBuildTasks), false);
            ValidateStepsList(BuildSourceTasks, nameof(BuildSourceTasks), true);
            ValidateStepsList(BuildOutputTasks, nameof(BuildOutputTasks), true);
            ValidateStepsList(PostBuildTasks, nameof(PostBuildTasks), false);
        }
        
        private void ValidateStepsList(IEnumerable<OeBuildStep> steps, string propertyNameOf, bool buildFromList) {
            var i = 0;
            foreach (var step in steps) {
                try {
                    if (string.IsNullOrEmpty(step.Label)) {
                        step.Label = $"Step {i}";
                    }
                    step.Validate(buildFromList);
                } catch (Exception e) {
                    var et = e as TaskValidationException;
                    if (et != null) {
                        et.StepNumber = i;
                        et.PropertyName = typeof(OeBuildConfiguration).GetXmlName(propertyNameOf);
                    }
                    throw new BuildConfigurationException(et != null ? et.Message : "Unexpected exception when checking tasks", et ?? e);
                }
                i++;
            }
        }

        public override string ToString() {
            return $"{(string.IsNullOrEmpty(ConfigurationName) ? "unnamed configuration" : $"configuration {ConfigurationName.PrettyQuote()}")}";
        }
    }

}