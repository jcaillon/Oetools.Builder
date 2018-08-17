using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeDatabaseAlias {
                
        [XmlAttribute(AttributeName = "DatabaseLogicalName")]
        public string DatabaseLogicalName { get; set; }
        
        [XmlAttribute(AttributeName = "AliasLogicalName")]
        public string AliasLogicalName { get; set; }
    }
}