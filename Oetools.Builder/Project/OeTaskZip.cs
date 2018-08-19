using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeTaskZip : OeTaskOnFilesWithTargetArchives {
        
        [XmlAttribute("TargetZipFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetZipFilePath { get; set; }
        
        [XmlAttribute(AttributeName = "ArchivesCompressionLevel")]
        public string ArchivesCompressionLevel { get; set; }

        public override OeCompressionLevel GetArchivesCompressionLevel() {
            if (Enum.TryParse(ArchivesCompressionLevel, true, out OeCompressionLevel level)) {
                return level;
            }
            return OeCompressionLevel.None;
        }
        
        public override string GetTargetArchive() => TargetZipFilePath;
    }
}