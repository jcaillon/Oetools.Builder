using System;
using System.Xml.Serialization;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeDatabaseAlias : IEnvExecutionDatabaseAlias {
                
        [XmlAttribute(AttributeName = "DatabaseLogicalName")]
        public string DatabaseLogicalName { get; set; }
        
        [XmlAttribute(AttributeName = "AliasLogicalName")]
        public string AliasLogicalName { get; set; }
    }
}