using System;
using System.Xml.Serialization;

namespace Oetools.Builder.History {
    [Serializable]
    public enum OeFileState : byte {
        [XmlEnum("Added")]
        Added,
        [XmlEnum("Replaced")]
        Replaced,
        [XmlEnum("Deleted")]
        Deleted,
        [XmlEnum("Existing")]
        Existing
    }
}