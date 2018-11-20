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

namespace Oetools.Builder.Project.Properties {
    
    /// <inheritdoc cref="OeBuildOptions.IncrementalBuildOptions"/>
    [Serializable]
    public class OeIncrementalBuildOptions {
                
        /// <summary>
        /// Sets whether or not the incremental build should be used.
        /// An incremental build improves the build process by only compiling and building files that were modified or added since the last build. It is the opposite of a full rebuild.
        /// </summary>
        /// <remarks>
        /// If true, an analysis is done on compiled files to find referenced tables and files. The build history is stored to be able to know which file was modified/added since the last build. And the MD5 checksum of each source file can be computed and saved to improve modification detection.
        /// Depending on your build and your intentions, this can significantly improve the build performances or slow down systematic full rebuilds.
        /// </remarks>
        [XmlElement(ElementName = "EnabledIncrementalBuild")]
        public bool? EnabledIncrementalBuild { get; set; }
        public static bool GetDefaultEnabledIncrementalBuild() => true;
            
        /// <summary>
        /// Use a cheapest analysis mode (performance wise) to identify the database references of a compiled file.
        /// </summary>
        /// <remarks>
        /// If true, the database references are not computed from the resulting xref file but simply using RCODE-INFO:TABLE-LIST.
        /// This method is less accurate because it will not list referenced sequences or referenced tables in LIKE TABLE statements. Even if a file does not need to be recompiled when a table referenced in a LIKE TABLE statement changes, it is smart to still recompile it and make sure that the database modification does not break the code.
        /// This option can be used on low-end computers but it is not advised to use this mode if it is not necessary.
        /// </remarks>
        [XmlElement(ElementName = "UseSimplerAnalysisForDatabaseReference")]
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
        public bool? UseCheckSumComparison { get; set; }
        public static bool GetDefaultUseCheckSumComparison() => false;
            
        /// <summary>
        /// Sets whether of not the tool should delete the previous targets of a source file that has been deleted since the last build.
        /// </summary>
        /// <example>
        /// On the first build, the file "A" was compiled and copied to location "/bin". The file "A" is deleted and a second build is started:
        /// - If this option is true, the compiled file "A" in "/bin/A" will be deleted
        /// - If not, nothing happens
        /// </example>
        [XmlElement(ElementName = "MirrorDeletedSourceFileToOutput")]
        public bool? MirrorDeletedSourceFileToOutput { get; set; }
        public static bool GetDefaultMirrorDeletedSourceFileToOutput() => false;
            
        /// <summary>
        /// Sets whether of not the tool should apply the modifications of a target that has been deleted since the last build. 
        /// </summary>
        /// <example>
        /// On the first build, the file "A" was compiled and copied to location "/bin" as well as "/bin2". The target "/bin" is deleted and a second build is started:
        /// - If this option is true, the compiled file "A" in "/bin/A" will be deleted
        /// - If not, nothing happens and the file "A" is not recompiled
        /// </example>
        [XmlElement(ElementName = "MirrorDeletedTargetsToOutput")]
        public bool? MirrorDeletedTargetsToOutput { get; set; }
        public static bool GetDefaultMirrorDeletedTargetsToOutput() => false;
            
        /// <summary>
        /// Sets whether of not the tool should rebuild a file if it has new targets defined since the last build.
        /// </summary>
        /// <example>
        /// On the first build, the file "A" was compiled and copied to location "/bin". A new target "/bin2" is added and a second build is started:
        /// - If this option is true, the file "A" is recompiled and built into "/bin2/A"
        /// - If not, nothing happens (the file "A" has not been changed since the last build)
        /// </example>
        [XmlElement(ElementName = "RebuildFilesWithNewTargets")]
        public bool? RebuildFilesWithNewTargets { get; set; }
        public static bool GetDefaultRebuildFilesWithNewTargets() => false;
        
        /// <summary>
        /// Is this incremental build active?
        /// </summary>
        /// <returns></returns>
        internal bool IsActive() => EnabledIncrementalBuild ?? GetDefaultEnabledIncrementalBuild();
    }
}