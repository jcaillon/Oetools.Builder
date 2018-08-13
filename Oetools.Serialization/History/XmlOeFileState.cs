using System;
using System.Xml.Serialization;

namespace Oetools.Serialization.History {
    [Serializable]
    public enum XmlOeFileState {
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