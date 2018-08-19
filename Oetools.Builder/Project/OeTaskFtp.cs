using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    [XmlRoot("Ftp")]
    public class OeTaskFtp : OeTaskOnFileWithTargetFiles {
    }
}