using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeTaskProlib : OeTaskOnFileArchive {
        
        [XmlAttribute("TargetProlibFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetProlibFilePath { get; set; }
        
        public override string GetTargetArchive() => TargetProlibFilePath;
    }
}