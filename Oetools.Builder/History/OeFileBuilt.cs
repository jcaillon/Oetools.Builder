using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeFileBuilt : OeFile {

        /// <summary>
        /// A list of the targets for this file
        /// </summary>
        [XmlArray("Targets")]
        [XmlArrayItem("Compiled", typeof(OeTargetCompile))]
        [XmlArrayItem("Copied", typeof(OeTargetCopy))]
        [XmlArrayItem("Prolibed", typeof(OeTargetProlib))]
        [XmlArrayItem("Zipped", typeof(OeTargetZip))]
        [XmlArrayItem("Cabbed", typeof(OeTargetCab))]
        public List<OeTarget> Targets { get; set; }
    }
}