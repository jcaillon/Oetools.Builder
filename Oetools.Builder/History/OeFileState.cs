using System;
using System.Xml.Serialization;

namespace Oetools.Builder.History {
    [Serializable]
    public enum OeFileState : byte {
        [XmlEnum("Added")]
        Added,
        [XmlEnum("Modified")]
        Modified,
        [XmlEnum("Deleted")]
        Deleted,
        [XmlEnum("Unchanged")]
        Unchanged
    }
}