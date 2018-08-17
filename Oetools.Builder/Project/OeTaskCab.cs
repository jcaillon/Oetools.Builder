using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    
    [Serializable]
    [XmlRoot("Cab")]
    public class OeTaskCab : OeTaskOnFileArchive {
        
        [XmlAttribute("TargetCabFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetCabFilePath { get; set; }
        
        [XmlElement(ElementName = "ArchivesCompressionLevel")]
        public OeCompressionLevel? ArchivesCompressionLevel { get; set; }

        public override string GetTargetArchive() => TargetCabFilePath;
    }
}