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
using Oetools.Builder.Report.Html;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder {
    
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
                        Log?.Info("Shutting down database after the build [OPTION]. This can take some time.");
                        _projectDbAdmin.ShutdownAllDatabases();
                    }
                } catch (Exception e) {
                    Log?.Error($"Error while shutting down the project databases: {e.Message}.", e);
                } finally {
                    _projectDbAdmin.Dispose();
                }
                try {
                    if (!string.IsNullOrEmpty(BuildConfiguration.Properties.BuildOptions.BuildConfigurationExportFilePath)) {
                        var exportProject = new OeProject {
                            BuildConfigurations = new List<OeBuildConfiguration> {
                                BuildConfiguration
                            }
                        };
                        Utils.CreateDirectoryIfNeeded(Path.GetDirectoryName(BuildConfiguration.Properties.BuildOptions.BuildConfigurationExportFilePath));
                        exportProject.Save(BuildConfiguration.Properties.BuildOptions.BuildConfigurationExportFilePath);
                    }
                } catch (Exception e) {
                    Log?.Error($"Error while writing the build configuration used: {e.Message}.", e);
                }
            }
        }

        protected override void PreBuild() {
            base.PreBuild();
            
            // Prepare all the project databases
            var databasesBaseDir = Path.Combine(OeBuilderConstants.GetProjectDirectoryLocalDb(BuildConfiguration.Properties.BuildOptions.SourceDirectoryPath), BuildConfiguration.Id.ToString());
            _projectDbAdmin = new ProjectDatabaseAdministrator(BuildConfiguration.Properties.DlcDirectory, BuildConfiguration.Properties.ProjectDatabases, databasesBaseDir) {
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
            if (UseIncrementalBuild && BuildSourceHistory == null && File.Exists(BuildConfiguration.Properties.BuildOptions.BuildHistoryInputFilePath)) {
                Log?.Debug("Loading the build history.");
                BuildSourceHistory = OeBuildHistory.Load(BuildConfiguration.Properties.BuildOptions.BuildHistoryInputFilePath, BuildConfiguration.Properties.BuildOptions.SourceDirectoryPath, BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath);
            }
        }

        protected override void PostBuild() {
            base.PostBuild();
            
            // output files
            if (!string.IsNullOrEmpty(BuildConfiguration.Properties.BuildOptions.ReportHtmlFilePath)) {
                OutputReport(BuildConfiguration.Properties.BuildOptions.ReportHtmlFilePath);
            }

            if (UseIncrementalBuild && !string.IsNullOrEmpty(BuildConfiguration.Properties.BuildOptions.BuildHistoryOutputFilePath)) {
                Log?.Debug("Create the output history file [OPTION].");
                OutputHistory(BuildConfiguration.Properties.BuildOptions.BuildHistoryOutputFilePath);
            }
        }
        
        private void OutputReport(string outputReportPath) {
            var reportExporter = new BuildReportExport(outputReportPath, this);
            //reportExporter.Create();
        }
        
        private void OutputHistory(string outputHistoryFilePath) {
            if (BuildSourceHistory == null) {
                return;
            }
            // BuildHistory
            Utils.CreateDirectoryIfNeeded(Path.GetDirectoryName(outputHistoryFilePath));
            BuildSourceHistory.Save(outputHistoryFilePath, BuildConfiguration.Properties.BuildOptions.SourceDirectoryPath, BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath);
        }
    }
}