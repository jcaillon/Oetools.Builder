using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public enum OeCompressionLevel {
        [XmlEnum("None")] None,
        [XmlEnum("Max")] Max
    }
}