using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeSourceFilter {
            
        [XmlAttribute("Exclude")]
        public string Exclude { get; set; }
    }
}