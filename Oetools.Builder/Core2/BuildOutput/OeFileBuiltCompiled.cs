using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Oetools.Builder.Core.Execution {
    [Serializable]
    public class OeFileBuiltCompiled : OeFileBuilt {
            
        /// <summary>
        /// Represents the source file (i.e. includes) used to generate a given .r code file
        /// </summary>
        [XmlArray("RequiredFiles")]
        [XmlArrayItem("RequiredFile", typeof(OeFile))]
        public List<OeFile> RequiredFiles { get; set; }

        /// <summary>
        ///     represent the tables that were referenced in a given .r code file
        /// </summary>
        [XmlArray("RequiredTables")]
        [XmlArrayItem("RequiredTable", typeof(OeTableCrc))]
        public List<OeTableCrc> RequiredTables { get; set; }
    }
}