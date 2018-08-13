using System;
using System.Xml.Serialization;

namespace Oetools.Serialization.Project {
    [Serializable]
    public class XmlOeSourceFilter {
            
        [XmlAttribute("Exclude")]
        public string Exclude { get; set; }
    }
}