using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Oetools.Packager.Core.Execution {
    [Serializable]
    public class OeFileBuilt : OeFile {
        /// <summary>
        /// Represents the state of the file for this build compare to the previous one
        /// </summary>
        [XmlElement(ElementName = "State")]
        public OeFileState State { get; set; }

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