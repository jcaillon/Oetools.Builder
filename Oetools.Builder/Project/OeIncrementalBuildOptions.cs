using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeIncrementalBuildOptions {
                
        /// <summary>
        /// If false, there will be no analyze of compiled files (ref tables/files), no storage
        /// of the build history after the build, no computation of MD5 nor comparison of date/size of files
        /// </summary>
        [XmlElement(ElementName = "Disabled")]
        public bool? Disabled { get; set; }
                
        /// <summary>
        /// True if the tool should use a checksum (md5) for each file to figure out if it has changed
        /// </summary>
        [XmlElement(ElementName = "StoreSourceHash")]
        public bool? StoreSourceHash { get; set; }
            
        /// <summary>
        /// If a source file has been deleted since the last build, should we try to delete it in the output directory
        /// if it still exists?
        /// </summary>
        [XmlElement(ElementName = "MirrorDeletedSourceFileToOutput")]
        public bool? MirrorDeletedSourceFileToOutput { get; set; }
    }
}