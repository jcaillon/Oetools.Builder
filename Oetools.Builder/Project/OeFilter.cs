using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    public class OeFilter { 
        [XmlAttribute(AttributeName = "Exclude")]
        public string Exclude { get; set; }
    }
}