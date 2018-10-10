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
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeBuildOptions {
        
        private string _sourceDirectoryPath;

        [XmlElement(ElementName = "SourceDirectoryPath")]
        public string SourceDirectoryPath {
            get => _sourceDirectoryPath;
            set => _sourceDirectoryPath = value.ToCleanPath();
        }
        [Description("$PWD (current directory)")]
        public static string GetDefaultSourceDirectoryPath() => Directory.GetCurrentDirectory();
        
        [XmlElement(ElementName = "OutputDirectoryPath")]
        public string OutputDirectoryPath { get; set; }

        [XmlElement(ElementName = "BuildHistoryOutputFilePath")]
        public string BuildHistoryOutputFilePath { get; set; }
            
        [XmlElement(ElementName = "BuildHistoryInputFilePath")]
        public string BuildHistoryInputFilePath { get; set; }
        
        [XmlElement(ElementName = "ReportHtmlFilePath")]
        public string ReportHtmlFilePath { get; set; }

        /// <summary>
        /// Should warnings be considered as errors and stop the build
        /// </summary>
        [XmlElement(ElementName = "TreatWarningsAsErrors")]
        public bool? TreatWarningsAsErrors { get; set; }
        public static bool GetDefaultTreatWarningsAsErrors() => false;
        
        /// <summary>
        /// Should the build be stopped if a file fails to compile
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnCompilationError")]
        public bool? StopBuildOnCompilationError { get; set; }
        public static bool GetDefaultStopBuildOnCompilationError() => true;
        
        /// <summary>
        /// Should the build be stopped if a file compiles with warnings
        /// </summary>
        [XmlElement(ElementName = "StopBuildOnCompilationWarning")]
        public bool? StopBuildOnCompilationWarning { get; set; }
        public static bool GetDefaultStopBuildOnCompilationWarning() => false;
        
        [XmlElement(ElementName = "ShutdownCompilationDatabasesAfterBuild")]
        public bool? ShutdownCompilationDatabasesAfterBuild { get; set; }
        public static bool GetDefaultShutdownCompilationDatabasesAfterBuild() => true;
        
        [XmlElement(ElementName = "TestMode")]
        public bool? TestMode { get; set; }
        public static bool GetDefaultTestMode() => false;
        
    }
}