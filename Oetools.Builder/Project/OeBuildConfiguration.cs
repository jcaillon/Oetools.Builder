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
using System.Xml.Schema;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Resources;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib.Attributes;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;

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
            interfaceXml.ConfigurationName = Path.GetFileNameWithoutExtension(path);
            interfaceXml.InitIds();
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
        
        [XmlIgnore]
        internal int Id { get; set; }
        
        /// <summary>
        /// Every existing sub node (even if empty) present in this will replace their <see cref="OeProject.GlobalProperties"/>
        /// counterpart
        /// </summary>
        [XmlElement("OverloadProperties")]
        [DeepCopy(Ignore = true)]
        public OeProperties Properties { get; set; }
            
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
            Variables = Variables ?? new List<OeVariable>();
            
            // add some default variables
            if (!string.IsNullOrEmpty(sourceDirectory)) {
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameSourceDirectory, Value = sourceDirectory });    
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameProjectDirectory, Value = OeBuilderConstants.GetProjectDirectory(sourceDirectory) });                
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameProjectLocalDirectory, Value = OeBuilderConstants.GetProjectDirectoryLocal(sourceDirectory) });              
            }             
            Variables.Add(new OeVariable { Name = UoeConstants.OeDlcEnvVar, Value = (Properties?.DlcDirectoryPath).TakeDefaultIfNeeded(OeProperties.GetDefaultDlcDirectoryPath()) });  
            Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameOutputDirectory, Value = Properties?.BuildOptions?.OutputDirectoryPath ?? OeBuilderConstants.GetDefaultOutputDirectory(sourceDirectory) });  
            Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameConfigurationName, Value = ConfigurationName });
            try {
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameCurrentDirectory, Value = Directory.GetCurrentDirectory() });
            } catch (Exception e) {
                throw new BuildConfigurationException(this, "Failed to get the current directory (check permissions)", e);
            }
            // extra variable FILE_SOURCE_DIRECTORY defined only when computing targets
            
            // apply variables on variables
            BuilderUtilities.ApplyVariablesInVariables(Variables);
            
            // apply variables in all public string properties
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
            ValidateStepsList(PreBuildTasks, nameof(PreBuildTasks), false);
            ValidateStepsList(BuildSourceTasks, nameof(BuildSourceTasks), true);
            ValidateStepsList(BuildOutputTasks, nameof(BuildOutputTasks), true);
            ValidateStepsList(PostBuildTasks, nameof(PostBuildTasks), false);
        }
        
        private void ValidateStepsList(IEnumerable<OeBuildStep> steps, string propertyNameOf, bool buildFromList) {
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

        /// <summary>
        /// Give each build step/variables a unique number to identify it
        /// </summary>
        internal void InitIds() {
            InitIds(PreBuildTasks);
            InitIds(BuildSourceTasks);
            InitIds(BuildOutputTasks);
            InitIds(PostBuildTasks);
            if (Variables != null) {
                var i = 0;
                foreach (var variable in Variables.Where(v => v != null)) {
                    variable.Id = i;
                    i++;
                }
            }
        }
        
        private void InitIds(IEnumerable<OeBuildStep> buildSteps) {
            if (buildSteps == null) {
                return;
            }
            var i = 0;
            foreach (var buildStep in buildSteps.Where(s => s != null)) {
                buildStep.Id = i;
                buildStep.InitIds();
                i++;
            }
        }
        
        public override string ToString() => $"Configuration [{Id}]{(string.IsNullOrEmpty(ConfigurationName) ? "" : $" {ConfigurationName}")}";

    }

}