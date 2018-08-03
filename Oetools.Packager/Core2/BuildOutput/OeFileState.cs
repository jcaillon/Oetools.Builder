using System;
using System.Xml.Serialization;

namespace Oetools.Packager.Core.Execution {
    [Serializable]
    public enum OeFileState {
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