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
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeBuildOptions {
                
        [XmlElement(ElementName = "OutputDirectoryPath")]
        public string OutputDirectoryPath { get; set; }
        internal static string GetDefaultOutputDirectoryPath(string sourceDirectory) => Path.Combine(sourceDirectory, "bin");

        [XmlElement(ElementName = "BuildHistoryOutputFilePath")]
        public string BuildHistoryOutputFilePath { get; set; }
        internal static string GetDefaultBuildHistoryOutputFilePath(string sourceDirectory) => Path.Combine(OeBuilderConstants.GetProjectDirectoryBuild(sourceDirectory), "latest.xml");
            
        [XmlElement(ElementName = "BuildHistoryInputFilePath")]
        public string BuildHistoryInputFilePath { get; set; }
        internal static string GetDefaultBuildHistoryInputFilePath(string sourceDirectory) => Path.Combine(OeBuilderConstants.GetProjectDirectoryBuild(sourceDirectory), "latest.xml");
        
        [XmlElement(ElementName = "ReportHtmlFilePath")]
        public string ReportHtmlFilePath { get; set; }
        internal static string GetDefaultReportHtmlFilePath(string sourceDirectory) => Path.Combine(OeBuilderConstants.GetProjectDirectoryBuild(sourceDirectory), "latest.html");

        /// <summary>
        /// Should warnings be considered as errors and stop the build
        /// </summary>
        [XmlElement(ElementName = "TreatWarningsAsErrors")]
        public bool? TreatWarningsAsErrors { get; set; }
        internal static bool GetDefaultTreatWarningsAsErrors() => false;
        
        /// <summary>
        /// Should the build be stopped if a file fails to compile
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnCompilationError")]
        public bool? StopBuildOnCompilationError { get; set; }
        internal static bool GetDefaultStopBuildOnCompilationError() => true;
        
        [XmlElement(ElementName = "ShutdownCompilationDatabasesAfterBuild")]
        public bool? ShutdownCompilationDatabasesAfterBuild { get; set; }
        internal static bool GetDefaultShutdownCompilationDatabasesAfterBuild() => true;
    }
}