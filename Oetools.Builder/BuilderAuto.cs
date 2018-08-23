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

using System.IO;
using Oetools.Builder.Project;
using Oetools.Builder.Report.Html;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder {
    
    public class BuilderAuto : Builder {

        public BuilderAuto(OeProject project, string buildConfigurationName = null) : base(project, buildConfigurationName) { }

        public BuilderAuto(OeBuildConfiguration buildConfiguration) : base(buildConfiguration) { }

//        BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath = BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath ?? Path.Combine(SourceDirectory, "bin");
//        public static string GetDefaultBuildHistoryOutputFilePath(string sourceDirectory) => Path.Combine(OeBuilderConstants.GetProjectDirectoryBuild(sourceDirectory), "latest.xml");
//        public static string GetDefaultBuildHistoryInputFilePath(string sourceDirectory) => Path.Combine(OeBuilderConstants.GetProjectDirectoryBuild(sourceDirectory), "latest.xml");
//        public static string GetDefaultReportHtmlFilePath(string sourceDirectory) => Path.Combine(OeBuilderConstants.GetProjectDirectoryBuild(sourceDirectory), "latest.html");

        protected override void PreBuild() {
            base.PreBuild();
            
            // Prepare all the project databases
            using (var dbAdmin = new ProjectDatabaseAdministrator(BuildConfiguration, SourceDirectory)) {
                var projectDbConnectionStrings = dbAdmin.SetupProjectDatabases();
                if (projectDbConnectionStrings != null && projectDbConnectionStrings.Count > 0) {
                    Log?.Debug("Adding project databases connection strings to the execution environment");
                    Env.DatabaseConnectionString = $"{Env.DatabaseConnectionString ?? ""} {string.Join(" ", projectDbConnectionStrings)}";
                    Log?.Debug($"The connection string is now {Env.DatabaseConnectionString}");
                }
            }
        }

        protected override void PostBuild() {
            base.PostBuild();
            
            // output files
            if (!string.IsNullOrEmpty(BuildConfiguration.Properties.BuildOptions?.ReportHtmlFilePath)) {
                OutputReport(BuildConfiguration.Properties.BuildOptions.ReportHtmlFilePath);
            }

            if (UseIncrementalBuild && !string.IsNullOrEmpty(BuildConfiguration.Properties.BuildOptions?.BuildHistoryOutputFilePath)) {
                Log?.Debug("Create the output history file [OPTION]");
                OutputHistory(BuildConfiguration.Properties.BuildOptions.BuildHistoryOutputFilePath);
            }
            
            // stop the started databases
            if (BuildConfiguration.Properties?.BuildOptions?.ShutdownCompilationDatabasesAfterBuild ?? OeBuildOptions.GetDefaultShutdownCompilationDatabasesAfterBuild()) {
                Log?.Debug("Shutting down database after the build [OPTION]");
                using (var dbAdmin = new ProjectDatabaseAdministrator(BuildConfiguration, SourceDirectory)) {
                    dbAdmin.ShutdownAllDatabases();
                }
            }
        }
        
        private void OutputReport(string outputReportPath) {
            var reportExporter = new BuildReportExport(outputReportPath, this);
            //reportExporter.Create();
        }
        
        private void OutputHistory(string outputHistoryFilePath) {
            if (BuildHistory == null || BuildSourceTaskExecutors == null) {
                return;
            }
            // BuildHistory
        }
    }
}