using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeTaskZip : OeTaskOnFileArchive {
        
        [XmlAttribute("TargetZipPath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetZipPath { get; set; }
        
        [XmlElement(ElementName = "ArchivesCompressionLevel")]
        public OeCompressionLevel ArchivesCompressionLevel { get; set; }
        
        public override string GetTargetArchive() => TargetZipPath;
    }
}