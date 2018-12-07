#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (BuilderAuto.cs) is part of Oetools.Builder.
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
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Report.Html;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder {
    
    /// <summary>
    /// Add key features to a <see cref="Builder"/>.
    /// </summary>
    public class BuilderAuto : Builder {

        private ProjectDatabaseAdministrator _projectDbAdmin;

        public BuilderAuto(OeProject project, string buildConfigurationName = null) : base(project, buildConfigurationName) { }

        public BuilderAuto(OeBuildConfiguration buildConfiguration) : base(buildConfiguration) { }

        public override void Dispose() {
            try {
                base.Dispose();
            } finally {
                try {
                    // stop the started databases
                    if (BuildConfiguration.Properties.BuildOptions.ShutdownCompilationDatabasesAfterBuild ?? OeBuildOptions.GetDefaultShutdownCompilationDatabasesAfterBuild()) {
                        Log?.Debug("Shutting down database after the build.");
                        _projectDbAdmin.ShutdownAllDatabases();
                    }
                } catch (Exception e) {
                    Log?.Error($"Error while shutting down the project databases: {e.Message}.", e);
                } finally {
                    _projectDbAdmin?.Dispose();
                }
            }
        }

        protected override void PreBuild() {
            base.PreBuild();
            
            // Prepare all the project databases
            var databasesBaseDir = Path.Combine(OeBuilderConstants.GetProjectDirectoryLocalDb(BuildConfiguration.Properties.BuildOptions.SourceDirectoryPath), BuildConfiguration.Id.ToString());
            _projectDbAdmin = new ProjectDatabaseAdministrator(BuildConfiguration.Properties.DlcDirectoryPath, BuildConfiguration.Properties.ProjectDatabases, databasesBaseDir) {
                Log = Log,
                AllowsDatabaseShutdownWithKill = BuildConfiguration.Properties.BuildOptions.AllowDatabaseShutdownByProcessKill,
                NumberOfUsersPerDatabase = OeCompilationOptions.GetNumberOfProcessesToUse(BuildConfiguration.Properties.CompilationOptions)
            };
            
            var projectDbConnectionStrings = _projectDbAdmin.SetupProjectDatabases();
            if (projectDbConnectionStrings != null && projectDbConnectionStrings.Count > 0) {
                Log?.Debug("Adding project databases connection strings to the execution environment.");
                var env = BuildConfiguration.Properties.GetEnv();
                env.DatabaseConnectionString = $"{env.DatabaseConnectionString ?? ""} {string.Join(" ", projectDbConnectionStrings)}";
                Log?.Debug($"The connection string is now {env.DatabaseConnectionString}.");
            }
            
            // load build history
            if (UseIncrementalBuild && BuildSourceHistory == null && File.Exists(BuildConfiguration.Properties.BuildOptions.IncrementalBuildOptions.BuildHistoryInputFilePath)) {
                Log?.Debug("Loading the build history.");
                BuildSourceHistory = OeBuildHistory.Load(BuildConfiguration.Properties.BuildOptions.IncrementalBuildOptions.BuildHistoryInputFilePath, BuildConfiguration.Properties.BuildOptions.SourceDirectoryPath, BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath);
            }
            
            // log input build configuration
            if (!string.IsNullOrEmpty(BuildConfiguration.Properties.BuildOptions.BuildConfigurationExportFilePath)) {
                var exportProject = new OeProject {
                    BuildConfigurations = new List<OeBuildConfiguration> {
                        BuildConfiguration
                    }
                };
                Utils.CreateDirectoryIfNeeded(Path.GetDirectoryName(BuildConfiguration.Properties.BuildOptions.BuildConfigurationExportFilePath));
                exportProject.Save(BuildConfiguration.Properties.BuildOptions.BuildConfigurationExportFilePath);
            }
        }

        protected override void PostBuild() {
            // output report
            if (!string.IsNullOrEmpty(BuildConfiguration.Properties.BuildOptions.ReportHtmlFilePath)) {
                OutputReport(BuildConfiguration.Properties.BuildOptions.ReportHtmlFilePath);
            }

            // output build history
            if (UseIncrementalBuild && !string.IsNullOrEmpty(BuildConfiguration.Properties.BuildOptions.IncrementalBuildOptions.BuildHistoryOutputFilePath)) {
                Log?.Debug($"Create the output history file: {BuildConfiguration.Properties.BuildOptions.IncrementalBuildOptions.BuildHistoryOutputFilePath}.");
                BuildSourceHistory = GetBuildHistory();
            
                // BuildHistory
                Utils.CreateDirectoryIfNeeded(Path.GetDirectoryName(BuildConfiguration.Properties.BuildOptions.IncrementalBuildOptions.BuildHistoryOutputFilePath));
                BuildSourceHistory.Save(BuildConfiguration.Properties.BuildOptions.IncrementalBuildOptions.BuildHistoryOutputFilePath, BuildConfiguration.Properties.BuildOptions.SourceDirectoryPath, BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath);
            }
        }
        
        private void OutputReport(string outputReportPath) {
            var reportExporter = new BuildReportExport(outputReportPath, this);
            //reportExporter.Create();
        }
    }
}