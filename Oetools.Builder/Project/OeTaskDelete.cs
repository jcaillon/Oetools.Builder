using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    [XmlRoot("Delete")]
    public class OeTaskDelete : OeTaskOnFiles {
    }
}