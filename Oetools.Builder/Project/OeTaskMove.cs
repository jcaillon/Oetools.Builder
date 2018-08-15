using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    [XmlRoot("Move")]
    public class OeTaskMove : OeTaskOnFileWithTarget {
    }
}