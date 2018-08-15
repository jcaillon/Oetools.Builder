using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeFileBuiltCompiled : OeFileBuilt {
        
        public OeFileBuiltCompiled(OeFile sourceFile) : base(sourceFile) { }
            
        /// <summary>
        /// Represents the source file (i.e. includes) used to generate a given .r code file
        /// </summary>
        [XmlArray("RequiredFiles")]
        [XmlArrayItem("RequiredFile", typeof(OeFile))]
        public List<OeFile> RequiredFiles { get; set; }

        /// <summary>
        ///     represent the tables that were referenced in a given .r code file
        /// </summary>
        [XmlArray("RequiredDatabaseReferences")]
        [XmlArrayItem("Table", typeof(OeDatabaseReferenceTable))]
        [XmlArrayItem("Sequence", typeof(OeDatabaseReferenceSequence))]
        public List<OeDatabaseReference> RequiredTables { get; set; }
        
    }
}