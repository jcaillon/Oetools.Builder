using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeVariable {
            
        [XmlAttribute("Name")]
        public string Name { get; set; }
            
        [XmlAttribute("Value")]
        public string Value { get; set; }
    }
}