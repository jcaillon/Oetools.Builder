using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeTaskProlib : OeTaskOnFileArchive {
        
        [XmlAttribute("TargetProlibPath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetProlibPath { get; set; }
        
        public override string GetTargetArchive() => TargetProlibPath;
    }
}