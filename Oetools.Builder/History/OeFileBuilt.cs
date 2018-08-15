using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeFileBuilt : OeFile {

        public OeFileBuilt(OeFile sourceFile) {
            Utils.DeepCopyPublicProperties(sourceFile, GetType(), this);
        }

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