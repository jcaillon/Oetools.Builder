// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (Builder.cs) is part of Oetools.Builder.
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

using System;
using System.Collections.Generic;
using System.IO;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder {
    
    public class Builder {
        
        private string _sourceDirectory;

        protected ILogger Log { get; set; }

        public string SourceDirectory {
            get => _sourceDirectory;
            set =>_sourceDirectory = value.ToCleanPath();
        }

        public OeBuildConfiguration BuildConfiguration { get; }
        
        public bool TestMode { get; set; }

        public bool ForceFullRebuild { get; set; }    
        
        public List<OeFileBuilt> PreviouslyBuiltFiles { get; set; }

        public Builder(OeProjectProperties projectProperties, OeBuildConfiguration buildConfiguration) {
            
            Log.Debug($"Initializing build with {(string.IsNullOrEmpty(buildConfiguration.ConfigurationName) ? "an unnamed configuration" : $"the configuration {buildConfiguration.ConfigurationName}")}");
            
            // make copies of received object since we want to modify them
            BuildConfiguration = (OeBuildConfiguration) Utils.DeepCopyPublicProperties(buildConfiguration, typeof(OeBuildConfiguration));
            
            // we can overload the project properties with the build configuration properties
            BuildConfiguration.Properties = (OeProjectProperties) Utils.DeepCopyPublicProperties(buildConfiguration.Properties, typeof(OeProjectProperties), projectProperties);
            BuildConfiguration.SanitizePathInPublicProperties();
        }
        
        public void Build() {
            Log.Info($"Start building {(string.IsNullOrEmpty(BuildConfiguration.ConfigurationName) ? "an unnamed configuration" : $"the configuration {BuildConfiguration.ConfigurationName}")}");

            Log.Debug("Checking build configuration");
            BuildConfiguration.Validate();
            
            Log.Debug("Using build variables");
            AddDefaultVariables();
            // replace variables
            BuilderUtilities.ApplyVariablesInVariables(BuildConfiguration.Variables);
            BuilderUtilities.ApplyVariablesToProperties(BuildConfiguration, BuildConfiguration.Variables);           
        }
        
        private void AddDefaultVariables() {
            if (BuildConfiguration.Variables == null) {
                BuildConfiguration.Variables = new List<OeVariable>();
            }
            if (BuildConfiguration.Properties.GlobalVariables != null) {
                // add global variables
                foreach (var globalVariable in BuildConfiguration.Properties.GlobalVariables) {
                    BuildConfiguration.Variables.Add(globalVariable);
                }
            }
            if (!string.IsNullOrEmpty(SourceDirectory)) {
                BuildConfiguration.Variables.Add(new OeVariable { Name = "SOURCE_DIRECTORY", Value = SourceDirectory });    
                BuildConfiguration.Variables.Add(new OeVariable { Name = "PROJECT_DIRECTORY", Value = Path.Combine(SourceDirectory, ".oe") });                
                BuildConfiguration.Variables.Add(new OeVariable { Name = "PROJECT_LOCAL_DIRECTORY", Value = Path.Combine(SourceDirectory, ".oe", "local") });                 
            }             
            BuildConfiguration.Variables.Add(new OeVariable { Name = "DLC", Value = BuildConfiguration.Properties.DlcDirectoryPath });  
            BuildConfiguration.Variables.Add(new OeVariable { Name = "OUTPUT_DIRECTORY", Value = BuildConfiguration.OutputDirectoryPath });  
            BuildConfiguration.Variables.Add(new OeVariable { Name = "CONFIGURATION_NAME", Value = BuildConfiguration.ConfigurationName });
            try {
                BuildConfiguration.Variables.Add(new OeVariable { Name = "WORKING_DIRECTORY", Value = Directory.GetCurrentDirectory() });
            } catch (Exception e) {
                Log.Error("Failed to get the current directory (check permissions)", e);
            }
            // extra variable FILE_SOURCE_DIRECTORY defined only when computing targets
        }
    }
}