#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeIncrementalBuildOptions.cs) is part of Oetools.Builder.
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
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Project.Properties {
    
    /// <inheritdoc cref="OeProperties.BuildOptions"/>
    [Serializable]
    public class OeBuildOptions {
        
        private string _sourceDirectoryPath;

        /// <summary>
        /// The source directory of your application. Should be left empty in most cases.
        /// </summary>
        /// <remarks>
        /// You cannot use variables in this property.
        /// </remarks>
        [XmlElement(ElementName = "SourceDirectoryPath")]
        [ReplaceVariables(SkipReplace = true)]
        public string SourceDirectoryPath {
            get => _sourceDirectoryPath;
            set => _sourceDirectoryPath = value.ToCleanPath();
        }
        [Description("$PWD (current directory)")]
        public static string GetDefaultSourceDirectoryPath() => Directory.GetCurrentDirectory().ToCleanPath();
        
        /// <summary>
        /// The filtering options for the source files of your application that need to be built.
        /// </summary>
        /// <remarks>
        /// For instance, this allows to exclude path from being considered as source files (e.g. a docs/ directory). Non source files will not be built during the source build tasks.
        /// </remarks>
        [XmlElement(ElementName = "SourceToBuildFilter")]
        public OeSourceFilterOptions SourceToBuildFilter { get; set; }
        
        /// <summary>
        /// The options for an incremental build.
        /// An incremental build improves the build process by only compiling and building files that were modified or added since the last build. It is the opposite of a full rebuild.
        /// </summary>
        /// <remarks>
        /// These options (and the incremental process in general) are only applicable to the tasks that are building your application sources.
        /// </remarks>
        [XmlElement(ElementName = "IncrementalBuildOptions")]
        public OeIncrementalBuildOptions IncrementalBuildOptions { get; set; }
        public static OeIncrementalBuildOptions GetDefaultIncrementalBuildOptions() => new OeIncrementalBuildOptions();
                
        /// <summary>
        /// Instead of a full rebuild or an incremental rebuild, use GIT to identify which files will be built.
        /// </summary>
        /// <remarks>
        /// This option required GIT to be installed and available in your system PATH.
        /// The idea behind this option is to build files depending on the changes made in GIT. For instance, rebuilding files modified/added since the last commit. It allows to check if recent changes in a GIT repository introduces bugs.
        /// </remarks>
        [XmlElement(ElementName = "SourceToBuildGitFilter")]
        public OeGitFilterOptions SourceToBuildGitFilter { get; set; }    
        public static OeGitFilterOptions GetDefaultSourceToBuildGitFilter() => new OeGitFilterOptions();
        
        /// <summary>
        /// Build all the source files, ignoring the incremental build options and the GIT filter options.
        /// </summary>
        [XmlElement(ElementName = "FullRebuild")]
        public bool? FullRebuild { get; set; }
        public static bool GetDefaultFullRebuild() => false;
        
        /// <summary>
        /// The output directory for the build.
        /// </summary>
        /// <remarks>
        /// Relative paths in the targets of tasks will be resolved from this directory. This is only available when building the source or the output.
        /// </remarks>
        [XmlElement(ElementName = "OutputDirectoryPath")]
        public string OutputDirectoryPath { get; set; }
        public static string GetDefaultOutputDirectoryPath() => OeBuilderConstants.GetDefaultOutputDirectory();
            
        /// <summary>
        /// The path to an xml file containing the information of a previous build. This is necessary for an incremental build.
        /// </summary>
        [XmlElement(ElementName = "BuildHistoryInputFilePath")]
        public string BuildHistoryInputFilePath { get; set; }
        public static string GetDefaultBuildHistoryInputFilePath() => OeBuilderConstants.GetDefaultBuildHistoryInputFilePath();

        /// <summary>
        /// The path to an xml file that will be created by this build and will contain the information of that build. This is necessary for an incremental build.
        /// </summary>
        [XmlElement(ElementName = "BuildHistoryOutputFilePath")]
        public string BuildHistoryOutputFilePath { get; set; }
        public static string GetDefaultBuildHistoryOutputFilePath() => OeBuilderConstants.GetDefaultBuildHistoryOutputFilePath();
        
        /// <summary>
        /// The path to an html report file that will contain human-readable information about this build.
        /// </summary>
        [XmlElement(ElementName = "ReportHtmlFilePath")]
        public string ReportHtmlFilePath { get; set; }
        public static string GetDefaultReportHtmlFilePath() => OeBuilderConstants.GetDefaultReportHtmlFilePath();
        
        /// <summary>
        /// The path to an xml file that will contain the exported build configuration for this build.
        /// </summary>
        [XmlElement(ElementName = "BuildConfigurationExportFilePath")]
        public string BuildConfigurationExportFilePath { get; set; }
        public static string GetDefaultBuildConfigurationExportFilePath() => OeBuilderConstants.GetDefaultBuildConfigurationExportFilePath();

        /// <summary>
        /// Sets whether or not the build must be stopped if a task generates warnings.
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnTaskError")]
        public bool? StopBuildOnTaskError { get; set; }
        public static bool GetDefaultStopBuildOnTaskError() => true;

        /// <summary>
        /// Sets whether or not the build must be stopped if a task generates warnings.
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnTaskWarning")]
        public bool? StopBuildOnTaskWarning { get; set; }
        public static bool GetDefaultStopBuildOnTaskWarning() => false;
        
        /// <summary>
        /// Sets whether or not the build must be stopped if a file fails to compile.
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnCompilationError")]
        public bool? StopBuildOnCompilationError { get; set; }
        public static bool GetDefaultStopBuildOnCompilationError() => true;
        
        /// <summary>
        /// Sets whether or not the build must be stopped if a file compiles with warnings.
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnCompilationWarning")]
        public bool? StopBuildOnCompilationWarning { get; set; }
        public static bool GetDefaultStopBuildOnCompilationWarning() => false;
        
        /// <summary>
        /// Sets whether or not the tool should shutdown the temporary databases created and started for the compilation (if any).
        /// </summary>
        /// <remarks>
        /// Shutting down an openedge database is a really slow process and it should be avoided if you intent to build files several times consecutively.
        /// </remarks>
        [XmlElement(ElementName = "ShutdownCompilationDatabasesAfterBuild")]
        public bool? ShutdownCompilationDatabasesAfterBuild { get; set; }
        public static bool GetDefaultShutdownCompilationDatabasesAfterBuild() => true;
        
        /// <summary>
        /// Sets whether or not the tool is allowed to shutdown temporary databases by killing the _mprosrv.
        /// </summary>
        /// <remarks>
        /// Shutting down an openedge database is a really slow process, this option speeds up the shutdown drastically. The database broker is killed instead of being properly shutdown. Since the tool uses temporary databases and since those databases are only used to compiled code, this is completely safe.
        /// </remarks>
        [XmlElement(ElementName = "AllowDatabaseShutdownByProcessKill")]
        public bool? AllowDatabaseShutdownByProcessKill { get; set; }
        public static bool GetDefaultAllowDatabaseShutdownByProcessKill() => true;
        
        /// <summary>
        /// Sets whether or not to run the build in "test mode". In test mode, the tasks are not actually executed. It should be used as a preview of the actual build process.
        /// </summary>
        [XmlElement(ElementName = "TestMode")]
        public bool? TestMode { get; set; }
        public static bool GetDefaultTestMode() => false;
        
    }
}