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

using System.Collections.Generic;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

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
        
        public EnvExecution Env { get; private set; }
        
        public List<OeFileBuilt> PreviouslyBuiltFiles { get; set; }
        
        public List<TaskExecutorOnFile> PreBuildTaskExecutors { get; set; }
        
        public List<TaskExecutorOnFileBuildingSource> BuildSourceTaskExecutors { get; set; }
        
        public List<TaskExecutorOnFile> BuildOutputTaskExecutors { get; set; }
        
        public List<TaskExecutorOnFile> PostBuildTaskExecutors { get; set; }

        /// <summary>
        /// Initiliaze the build
        /// </summary>
        /// <param name="project"></param>
        /// <param name="buildConfigurationName"></param>
        public Builder(OeProject project, string buildConfigurationName = null) {
            // make a copy of the build configuration
            BuildConfiguration = project.GetBuildConfigurationCopy(buildConfigurationName) ?? project.GetDefaultBuildConfigurationCopy();
            
            Log.Debug($"Initializing build with {BuildConfiguration}");
            
            Env = new EnvExecution {
                TempDirectory = string.IsNullOrEmpty(BuildConfiguration.Properties.TemporaryDirectoryPath) ? $".oe_tmp-{Utils.GetRandomName()}" : BuildConfiguration.Properties.TemporaryDirectoryPath,
                UseProgressCharacterMode = BuildConfiguration.Properties.UseCharacterModeExecutable ?? false,
                
            };
        }
        
        /// <summary>
        /// Main method, builds
        /// </summary>
        public void Build() {
            Log.Info($"Start building {(string.IsNullOrEmpty(BuildConfiguration.ConfigurationName) ? "an unnamed configuration" : $"the configuration {BuildConfiguration.ConfigurationName}")}");

            Log.Debug("Validating tasks");
            BuildConfiguration.ValidateAllTasks();
            
            Log.Debug("Using build variables");
            BuildConfiguration.ApplyVariables(SourceDirectory);
            
            Log.Debug("Sanitizing path properties");
            BuildConfiguration.Properties.SanitizePathInPublicProperties();
            
            ExecuteBuild();
        }

        private void ExecuteBuild() {           
            if (BuildConfiguration.PostBuildTasks != null) {
                var i = 0;
                foreach (var step in BuildConfiguration.PostBuildTasks) {
                    Log.Debug($"{typeof(OeBuildConfiguration).GetXmlName(nameof(OeBuildConfiguration.PostBuildTasks))} step {i}{(!string.IsNullOrEmpty(step.Label) ? $" : {step.Label}" : "")}");
                    var executor = new TaskExecutorOnFile(step.GetTaskList());
                    (PostBuildTaskExecutors ?? (PostBuildTaskExecutors = new List<TaskExecutorOnFile>())).Add(executor);
                }
            }
        }

        
    }
}