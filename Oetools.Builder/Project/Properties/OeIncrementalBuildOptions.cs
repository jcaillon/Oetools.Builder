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
using System.Xml.Serialization;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Attributes;

namespace Oetools.Builder.Project.Properties {
    
    /// <inheritdoc cref="OeBuildOptions.IncrementalBuildOptions"/>
    [Serializable]
    public class OeIncrementalBuildOptions {
                
        /// <inheritdoc cref="OeBuildOptions.IncrementalBuildOptions"/>
        [XmlElement(ElementName = "EnabledIncrementalBuild")]
        [DefaultValueMethod(nameof(GetDefaultEnabledIncrementalBuild))]
        public bool? EnabledIncrementalBuild { get; set; }
        public static bool GetDefaultEnabledIncrementalBuild() => true;
            
        /// <summary>
        /// The path to an xml file containing the information of the previous build. This is necessary for an incremental build.
        /// </summary>
        [XmlElement(ElementName = "BuildHistoryInputFilePath")]
        [DefaultValueMethod(nameof(GetDefaultBuildHistoryInputFilePath))]
        public string BuildHistoryInputFilePath { get; set; }
        public static string GetDefaultBuildHistoryInputFilePath() => OeBuilderConstants.GetDefaultBuildHistoryInputFilePath();

        /// <summary>
        /// The path to an xml file that will be created by this build and which will contain the information of that build. This is only generated after an incremental build.
        /// </summary>
        [XmlElement(ElementName = "BuildHistoryOutputFilePath")]
        [DefaultValueMethod(nameof(GetDefaultBuildHistoryOutputFilePath))]
        public string BuildHistoryOutputFilePath { get; set; }
        public static string GetDefaultBuildHistoryOutputFilePath() => OeBuilderConstants.GetDefaultBuildHistoryOutputFilePath();
            
        /// <summary>
        /// Use a cheapest analysis mode (performance wise) to identify the database references of a compiled file.
        /// </summary>
        /// <remarks>
        /// If true, the database references are not computed from the resulting xref file but simply using RCODE-INFO:TABLE-LIST.
        /// This method is less accurate because it will not list referenced sequences or referenced tables in LIKE TABLE statements. Even if a file does not need to be recompiled when a table referenced in a LIKE TABLE statement changes, it is smart to still recompile it and make sure that the database modification does not break the code.
        /// This option can be used on low-end computers but it is not advised to use this mode if it is not necessary.
        /// </remarks>
        [XmlElement(ElementName = "UseSimplerAnalysisForDatabaseReference")]
        [DefaultValueMethod(nameof(GetDefaultUseSimplerAnalysisForDatabaseReference))]
        public bool? UseSimplerAnalysisForDatabaseReference { get; set; }
        public static bool GetDefaultUseSimplerAnalysisForDatabaseReference() => false;
                
        /// <summary>
        /// Use a checksum comparison to identify the files that were modified between two builds.
        /// </summary>
        /// <remarks>
        /// This identification technique is safer than a simple datetime/size comparison but it also costs more cpu time.
        /// By default, a file is considered unmodified if its size and last modified date has not changed since the previous build. This option also compute and consider the file checksum using the MD5 checksum computation.
        /// </remarks>
        [XmlElement(ElementName = "UseCheckSumComparison")]
        [DefaultValueMethod(nameof(GetDefaultUseCheckSumComparison))]
        public bool? UseCheckSumComparison { get; set; }
        public static bool GetDefaultUseCheckSumComparison() => false;
            
        /// <summary>
        /// Sets whether of not the tool should rebuild a file if it has new targets defined since the last build.
        /// </summary>
        /// <example>
        /// On the first build, the file "A" was compiled and copied to location "/bin". A new target "/bin2" is added and a second build is started:
        /// - If this option is true, the file "A" is recompiled and built into "/bin2/A".
        /// - If not, nothing happens (the file "A" has not been changed since the last build).
        /// </example>
        [XmlElement(ElementName = "RebuildFilesWithNewTargets")]
        [DefaultValueMethod(nameof(GetDefaultRebuildFilesWithNewTargets))]
        public bool? RebuildFilesWithNewTargets { get; set; }
        public static bool GetDefaultRebuildFilesWithNewTargets() => false;
        
        /// <summary>
        /// Sets whether of not the tool should rebuild a file if it some of its targets are missing (the file does not exist anymore).
        /// </summary>
        /// <example>
        /// On the first build, the file "A" was compiled and copied to location "/bin". The file "A" is deleted and a second build is started:
        /// - If this option is true, the file "A" is recompiled and built into "/bin/A".
        /// - If not, nothing happens.
        /// </example>
        [XmlElement(ElementName = "RebuildFilesWithMissingTargets")]
        [DefaultValueMethod(nameof(GetDefaultRebuildFilesWithMissingTargets))]
        public bool? RebuildFilesWithMissingTargets { get; set; }
        public static bool GetDefaultRebuildFilesWithMissingTargets() => false;
            
        /// <summary>
        /// Sets whether of not the tool should try to rebuild a file if it had compilation errors in the previous build.
        /// </summary>
        /// <remarks>
        /// If the file has not been modified since the last build, there is little to no chance that it will successfully compile in the current build.
        /// But this can happen if the file didn't compile because the compilation database was outdated (missing a table needed in the procedure for instance).
        /// </remarks>
        [XmlElement(ElementName = "RebuildFilesWithCompilationErrors")]
        [DefaultValueMethod(nameof(GetDefaultRebuildFilesWithCompilationErrors))]
        public bool? RebuildFilesWithCompilationErrors { get; set; }
        public static bool GetDefaultRebuildFilesWithCompilationErrors() => true;
        
        /// <summary>
        /// Is this incremental build active?
        /// </summary>
        /// <returns></returns>
        internal bool IsActive() => EnabledIncrementalBuild ?? GetDefaultEnabledIncrementalBuild();
    }
}