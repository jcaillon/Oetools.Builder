using System.Xml.Serialization;

namespace Oetools.Serialization.Project {
    public abstract class XmlOeTaskOnFileWithTarget : XmlOeTaskOnFile {
            
        [XmlAttribute("Target")]
        public string Target { get; set; }
    }
}