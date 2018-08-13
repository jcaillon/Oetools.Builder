using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    public abstract class OeTaskOnFileWithTarget : OeTaskOnFile {
            
        [XmlAttribute("Target")]
        public string Target { get; set; }
    }
}