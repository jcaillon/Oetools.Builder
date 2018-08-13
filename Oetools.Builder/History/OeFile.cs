using System;
using System.Xml.Serialization;

namespace Oetools.Builder.History {
    
    [Serializable]
    public class OeFile {
        
        /// <summary>
        /// The relative path of the source file
        /// </summary>
        [XmlAttribute(AttributeName = "SourcePath")]
        public string SourcePath { get; set; }

        [XmlAttribute(AttributeName = "LastWriteTime")]
        public DateTime LastWriteTime { get; set; }

        [XmlAttribute(AttributeName = "Size")]
        public long Size { get; set; }

        /// <summary>
        ///     MD5
        /// </summary>
        [XmlAttribute(AttributeName = "Md5")]
        public string Md5 { get; set; }
        
        /// <summary>
        /// Represents the state of the file for this build compare to the previous one
        /// </summary>
        [XmlElement(ElementName = "State")]
        public OeFileState State { get; set; }
    }
}