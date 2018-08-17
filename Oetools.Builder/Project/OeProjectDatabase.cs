using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeProjectDatabase {
      
        [XmlAttribute(AttributeName = "LogicalName")]
        public string LogicalName { get; set; }
            
        [XmlAttribute(AttributeName = "DataDefinitionFilePath")]
        public string DataDefinitionFilePath { get; set; }
        
    }
}