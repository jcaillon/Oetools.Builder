#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeBuilderConstants.cs) is part of Oetools.Builder.
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

namespace Oetools.Builder.Utilities {
    public static class OeBuilderConstants {

        public const string VcsDirectoryExclusions = ".git**;.svn**";
        
        public const string CompilableExtensionsPattern = "*.p;*.w;*.t;*.cls";
        
        public const string OeProjectExtension = ".oeproj";

        public const string OeProjectDirectory = ".oe";
        
        private const string OeProjectLocalDirectory = "local";
        private const string OeProjectOutputDirectory = "bin";
        private const string OeProjectBuildDirectory = "build";
        
        public const string OeVarNameSourceDirectory = "SOURCE_DIRECTORY";
        public const string OeVarNameProjectDirectory = "PROJECT_DIRECTORY";
        public const string OeVarNameProjectLocalDirectory = "PROJECT_LOCAL_DIRECTORY";
        public const string OeVarNameOutputDirectory = "OUTPUT_DIRECTORY";
        public const string OeVarNameConfigurationName = "CONFIGURATION_NAME";
        public const string OeVarNameCurrentDirectory = "CURRENT_DIRECTORY";
        public const string OeVarNameFileSourceDirectory = "FILE_SOURCE_DIRECTORY";

        public static string GetProjectDirectory(string sourceDirectory) => Path.Combine(sourceDirectory, OeProjectDirectory);
        public static string GetProjectDirectoryLocal(string sourceDirectory) => Path.Combine(sourceDirectory, OeProjectDirectory, OeProjectLocalDirectory);
        public static string GetProjectDirectoryLocalDb(string sourceDirectory) => Path.Combine(sourceDirectory, OeProjectDirectory, OeProjectLocalDirectory, "db");
        
        public static string GetDefaultOutputDirectory() => Path.Combine($"{{{{{OeVarNameSourceDirectory}}}}}", OeProjectOutputDirectory);
        public static string GetDefaultBuildHistoryInputFilePath() => Path.Combine($"{{{{{OeVarNameProjectLocalDirectory}}}}}", OeProjectBuildDirectory, "latest.xml");
        public static string GetDefaultBuildHistoryOutputFilePath() => Path.Combine($"{{{{{OeVarNameProjectLocalDirectory}}}}}", OeProjectBuildDirectory, "latest.xml");
        public static string GetDefaultReportHtmlFilePath() => Path.Combine($"{{{{{OeVarNameProjectLocalDirectory}}}}}", OeProjectBuildDirectory, "latest.html");
    }
}