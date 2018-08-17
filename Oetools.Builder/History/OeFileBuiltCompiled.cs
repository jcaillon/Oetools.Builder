using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeFileBuiltCompiled : OeFileBuilt {
        public OeFileBuiltCompiled() { }

        public OeFileBuiltCompiled(OeFile sourceFile) : base(sourceFile) { }
            
        /// <summary>
        /// Represents all the source files that were used when compiling the original source file
        /// (for instance includes or interfaces)
        /// </summary>
        [XmlArray("RequiredFiles")]
        [XmlArrayItem("RequiredFile", typeof(string))]
        [BaseDirectory(Type = BaseDirectoryType.SourceDirectory)]
        public List<string> RequiredFiles { get; set; }

        /// <summary>
        /// Represents all the database entities referenced in the original source file and used for the compilation
        /// (can be sequences or tables)
        /// </summary>
        [XmlArray("RequiredDatabaseReferences")]
        [XmlArrayItem("Table", typeof(OeDatabaseReferenceTable))]
        [XmlArrayItem("Sequence", typeof(OeDatabaseReferenceSequence))]
        [BaseDirectory(SkipReplace = true)]
        public List<OeDatabaseReference> RequiredDatabaseReferences { get; set; }
        
    }
}