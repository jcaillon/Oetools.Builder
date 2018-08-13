using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Utilities.Openedge;

namespace Oetools.Serialization.History {
    [Serializable]
    public class XmlOeFileBuiltCompiled : XmlOeFileBuilt {
            
        /// <summary>
        /// Represents the source file (i.e. includes) used to generate a given .r code file
        /// </summary>
        [XmlArray("RequiredFiles")]
        [XmlArrayItem("RequiredFile", typeof(XmlOeFile))]
        public List<XmlOeFile> RequiredFiles { get; set; }

        /// <summary>
        ///     represent the tables that were referenced in a given .r code file
        /// </summary>
        [XmlArray("RequiredDatabaseReferences")]
        [XmlArrayItem("Table", typeof(XmlOeDatabaseReferenceTable))]
        [XmlArrayItem("Sequence", typeof(XmlOeDatabaseReferenceSequence))]
        public List<XmlOeDatabaseReference> RequiredTables { get; set; }
    }
}