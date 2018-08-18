﻿using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    
    [Serializable]
    [XmlRoot("Cab")]
    public class OeTaskCab : OeTaskOnFileArchive {
        
        [XmlAttribute("TargetCabFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetCabFilePath { get; set; }
        
        [XmlAttribute(AttributeName = "ArchivesCompressionLevel")]
        public string ArchivesCompressionLevel { get; set; }

        public OeCompressionLevel GetArchivesCompressionLevel() {
            if (Enum.TryParse(ArchivesCompressionLevel, true, out OeCompressionLevel level)) {
                return level;
            }
            return OeCompressionLevel.None;
        }

        public override string GetTargetArchive() => TargetCabFilePath;
    }
}