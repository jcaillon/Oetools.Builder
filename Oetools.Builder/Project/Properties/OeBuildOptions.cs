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
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Attributes;
using Oetools.Utilities.Lib.Extension;

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
        [DefaultValueMethod(nameof(GetDefaultSourceDirectoryPath))]
        public string SourceDirectoryPath {
            get => _sourceDirectoryPath;
            set => _sourceDirectoryPath = value.ToCleanPath();
        }
        [Description("$PWD (current directory)")]
        public static string GetDefaultSourceDirectoryPath() => Directory.GetCurrentDirectory().ToCleanPath();
        
        /// <summary>
        /// The output directory for the build.
        /// </summary>
        /// <remarks>
        /// Relative paths in the targets of tasks will be resolved from this directory. This is only available when building the source or the output.
        /// </remarks>
        [XmlElement(ElementName = "OutputDirectoryPath")]
        [DefaultValueMethod(nameof(GetDefaultOutputDirectoryPath))]
        public string OutputDirectoryPath { get; set; }
        public static string GetDefaultOutputDirectoryPath() => OeBuilderConstants.GetDefaultOutputDirectory();
        
        /// <summary>
        /// The filtering options for the source files of your application that need to be built.
        /// </summary>
        /// <remarks>
        /// For instance, this allows to exclude path from being considered as source files (e.g. a docs/ directory). Non source files will not be built during the source build tasks.
        /// </remarks>
        [XmlElement(ElementName = "SourceToBuildFilter")]
        [DefaultValueMethod(nameof(GetDefaultSourceToBuildFilter))]
        public OeSourceFilterOptions SourceToBuildFilter { get; set; }
        public static OeSourceFilterOptions GetDefaultSourceToBuildFilter() => new OeSourceFilterOptions();
        
        /// <summary>
        /// Sets whether or not the incremental build should be used.
        /// An incremental build improves the build process by only compiling and building source files that were modified or added since the last build. It is the opposite of a full rebuild.
        /// </summary>
        /// <remarks>
        /// - The incremental build only concerns source files handled in a source build step.
        /// - If true, a build history is stored (in .xml format) to be able to know which file has been modified/added since the last build.
        /// - An analysis is done on compiled files to find referenced tables and files.
        /// - The MD5 checksum of each source file can be computed and saved to improve modification detection.
        /// - Depending on your build and your intentions, this can significantly improve the build performances or slow down systematic full rebuilds.
        /// </remarks>
        [XmlElement(ElementName = "IncrementalBuildOptions")]
        [DefaultValueMethod(nameof(GetDefaultIncrementalBuildOptions))]
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
        [DefaultValueMethod(nameof(GetDefaultSourceToBuildGitFilter))]
        public OeGitFilterOptions SourceToBuildGitFilter { get; set; }    
        public static OeGitFilterOptions GetDefaultSourceToBuildGitFilter() => new OeGitFilterOptions();
        
        /// <summary>
        /// Build all the source files, ignoring the incremental build options and the GIT filter options.
        /// </summary>
        [XmlElement(ElementName = "FullRebuild")]
        [DefaultValueMethod(nameof(GetDefaultFullRebuild))]
        public bool? FullRebuild { get; set; }
        public static bool GetDefaultFullRebuild() => false;
        
        /// <summary>
        /// The path to an html report file that will contain human-readable information about this build.
        /// </summary>
        [XmlElement(ElementName = "ReportHtmlFilePath")]
        [DefaultValueMethod(nameof(GetDefaultReportHtmlFilePath))]
        public string ReportHtmlFilePath { get; set; }
        public static string GetDefaultReportHtmlFilePath() => OeBuilderConstants.GetDefaultReportHtmlFilePath();
        
        /// <summary>
        /// The path to an xml file that will contain the exported build configuration for this build.
        /// </summary>
        [XmlElement(ElementName = "BuildConfigurationExportFilePath")]
        [DefaultValueMethod(nameof(GetDefaultBuildConfigurationExportFilePath))]
        public string BuildConfigurationExportFilePath { get; set; }
        public static string GetDefaultBuildConfigurationExportFilePath() => OeBuilderConstants.GetDefaultBuildConfigurationExportFilePath();

        // TODO : test StopBuildOnTaskError
        /// <summary>
        /// Sets whether or not the build must be stopped if a task generates warnings.
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnTaskError")]
        [DefaultValueMethod(nameof(GetDefaultStopBuildOnTaskError))]
        public bool? StopBuildOnTaskError { get; set; }
        public static bool GetDefaultStopBuildOnTaskError() => true;

        /// <summary>
        /// Sets whether or not the build must be stopped if a task generates warnings.
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnTaskWarning")]
        [DefaultValueMethod(nameof(GetDefaultStopBuildOnTaskWarning))]
        public bool? StopBuildOnTaskWarning { get; set; }
        public static bool GetDefaultStopBuildOnTaskWarning() => false;
        
        /// <summary>
        /// Sets whether or not the build must be stopped if a file fails to compile.
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnCompilationError")]
        [DefaultValueMethod(nameof(GetDefaultStopBuildOnCompilationError))]
        public bool? StopBuildOnCompilationError { get; set; }
        public static bool GetDefaultStopBuildOnCompilationError() => true;
        
        /// <summary>
        /// Sets whether or not the build must be stopped if a file compiles with warnings.
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnCompilationWarning")]
        [DefaultValueMethod(nameof(GetDefaultStopBuildOnCompilationWarning))]
        public bool? StopBuildOnCompilationWarning { get; set; }
        public static bool GetDefaultStopBuildOnCompilationWarning() => false;
        
        /// <summary>
        /// Sets whether or not the tool should shutdown the temporary databases created and started for the compilation (if any).
        /// </summary>
        /// <remarks>
        /// Shutting down an openedge database is a really slow process and it should be avoided if you intent to build files several times consecutively.
        /// </remarks>
        [XmlElement(ElementName = "ShutdownCompilationDatabasesAfterBuild")]
        [DefaultValueMethod(nameof(GetDefaultShutdownCompilationDatabasesAfterBuild))]
        public bool? ShutdownCompilationDatabasesAfterBuild { get; set; }
        public static bool GetDefaultShutdownCompilationDatabasesAfterBuild() => true;
        
        /// <summary>
        /// Sets whether or not the tool is allowed to shutdown temporary databases by killing the _mprosrv.
        /// </summary>
        /// <remarks>
        /// Shutting down an openedge database is a really slow process, this option speeds up the shutdown drastically. The database broker is killed instead of being properly shutdown. Since the tool uses temporary databases and since those databases are only used to compiled code, this is completely safe.
        /// </remarks>
        [XmlElement(ElementName = "AllowDatabaseShutdownByProcessKill")]
        [DefaultValueMethod(nameof(GetDefaultAllowDatabaseShutdownByProcessKill))]
        public bool? AllowDatabaseShutdownByProcessKill { get; set; }
        public static bool GetDefaultAllowDatabaseShutdownByProcessKill() => true;
        
        /// <summary>
        /// Sets whether or not to run the build in "test mode". In test mode, the tasks are not actually executed. It should be used as a preview of the actual build process.
        /// </summary>
        [XmlElement(ElementName = "TestMode")]
        [DefaultValueMethod(nameof(GetDefaultTestMode))]
        public bool? TestMode { get; set; }
        public static bool GetDefaultTestMode() => false;
        
        /// <summary>
        /// Validate that is object is correct.
        /// </summary>
        /// <exception cref="PropertiesException"></exception>
        public void Validate() {
            try {
                SourceToBuildFilter?.Validate();
            } catch (Exception e) {
                throw new PropertiesException($"The property {nameof(SourceToBuildFilter)} has errors: {e.Message}", e);
            }
            if ((IncrementalBuildOptions?.IsActive() ?? false) && (SourceToBuildGitFilter?.IsActive() ?? false)) {
                throw new PropertiesException($"The {GetType().GetXmlName(nameof(IncrementalBuildOptions))} can not be active when the {GetType().GetXmlName(nameof(SourceToBuildGitFilter))} is active because the two options serve contradictory purposes. {GetType().GetXmlName(nameof(IncrementalBuildOptions))} should be used when the goal is to build the latest modifications on top of a previous build. {GetType().GetXmlName(nameof(SourceToBuildGitFilter))} should be used when the goal is to verify that recent commits to the git repo did not introduce bugs.");
            }
        }
    }
}