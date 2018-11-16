﻿#region header
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
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
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
        public string ConfigurationName { get; set; }
        
        /// <summary>
        /// The properties of this build configuration.
        /// </summary>
        /// <inheritdoc cref="OeProject.DefaultProperties"/>
        [XmlElement("Properties")]
        [DeepCopy(Ignore = true)]
        public OeProperties Properties { get; set; }
            
        /// <summary>
        /// The variables specific to this build configuration.
        /// </summary>
        /// <inheritdoc cref="OeProject.GlobalVariables"/>
        [XmlArray("Variables")]
        [XmlArrayItem("Variable", typeof(OeVariable))]
        [ReplaceVariables(SkipReplace = true)]
        public List<OeVariable> Variables { get; set; }
        
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
            Variables.Add(new OeVariable { Name = UoeConstants.OeDlcEnvVar, Value = (Properties?.DlcDirectory).TakeDefaultIfNeeded(OeProperties.GetDefaultDlcDirectory()) });  
            Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameOutputDirectory, Value = (Properties?.BuildOptions?.OutputDirectoryPath).TakeDefaultIfNeeded(OeBuilderConstants.GetDefaultOutputDirectory()) });  
            Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameConfigurationName, Value = ConfigurationName });
            try {
                Variables.Add(new OeVariable { Name = OeBuilderConstants.OeVarNameCurrentDirectory, Value = Directory.GetCurrentDirectory() });
            } catch (Exception e) {
                throw new BuildConfigurationException(this, "Failed to get the current directory (check permissions).", e);
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
            ValidateStepsList(PreBuildStepGroup, nameof(PreBuildStepGroup), false);
            ValidateStepsList(BuildSourceStepGroup, nameof(BuildSourceStepGroup), true);
            ValidateStepsList(BuildOutputStepGroup, nameof(BuildOutputStepGroup), true);
            ValidateStepsList(PostBuildStepGroup, nameof(PostBuildStepGroup), false);
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
            InitIds(PreBuildStepGroup);
            InitIds(BuildSourceStepGroup);
            InitIds(BuildOutputStepGroup);
            InitIds(PostBuildStepGroup);
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