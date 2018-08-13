using System;
using System.Xml.Serialization;

namespace Oetools.Serialization.History {
    /// <summary>
    ///     This class represent the tables that were referenced in a given .r code file
    /// </summary>
    [Serializable]
    public class XmlOeTableCrc {
        [XmlAttribute(AttributeName = "QualifiedTableName")]
        public string QualifiedTableName { get; set; }
            
        [XmlAttribute(AttributeName = "Crc")]
        public string Crc { get; set; }
    }
}