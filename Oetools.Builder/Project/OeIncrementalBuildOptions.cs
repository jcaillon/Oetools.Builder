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

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeIncrementalBuildOptions {
                
        /// <summary>
        /// If false, there will be no analyze of compiled files (ref tables/files), no storage
        /// of the build history after the build, no computation of MD5 nor comparison of date/size of files
        /// </summary>
        [XmlElement(ElementName = "Enabled")]
        public bool? Enabled { get; set; }
        public static bool GetDefaultEnabled() => true;
        
        [XmlElement(ElementName = "FullRebuild")]
        public bool? FullRebuild { get; set; }
        public static bool GetDefaultFullRebuild() => false;
                
        /// <summary>
        /// True if the tool should use a checksum (md5) for each file to figure out if it has changed
        /// </summary>
        [XmlElement(ElementName = "StoreSourceHash")]
        public bool? StoreSourceHash { get; set; }
        public static bool GetDefaultStoreSourceHash() => false;
            
        /// <summary>
        /// If a source file has been deleted since the last build, should we try to delete all its previous targets
        /// </summary>
        [XmlElement(ElementName = "MirrorDeletedSourceFileToOutput")]
        public bool? MirrorDeletedSourceFileToOutput { get; set; }
        public static bool GetDefaultMirrorDeletedSourceFileToOutput() => false;
            
        /// <summary>
        /// If a target has been deleted since the last build, should we try to apply those modifications.
        /// by default, if a file was not modified, we just don't build it, even if its targets have changed
        /// also, if a file is modified and we changed its targets since the last build, 
        /// </summary>
        [XmlElement(ElementName = "MirrorDeletedTargetsToOutput")]
        public bool? MirrorDeletedTargetsToOutput { get; set; }
        public static bool GetDefaultMirrorDeletedTargetsToOutput() => false;
            
        /// <summary>
        /// If a target has been added since the last build, should we try to apply those modifications.
        /// by default, if a file was not modified, we just don't build it, even if its targets have changed
        /// also, if a file is modified and we changed its targets since the last build, 
        /// </summary>
        [XmlElement(ElementName = "RebuildFilesWithNewTargets")]
        public bool? RebuildFilesWithNewTargets { get; set; }
        public static bool GetDefaultRebuildFilesWithNewTargets() => false;
        
        /// <summary>
        /// Is this incremental build active
        /// </summary>
        /// <returns></returns>
        internal bool IsActive() => Enabled ?? GetDefaultEnabled();
    }
}