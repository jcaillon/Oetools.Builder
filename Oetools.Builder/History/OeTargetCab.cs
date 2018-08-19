using System;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeTargetCab : OeTargetPack {
        
        /// <summary>
        /// Relative path inside the archive
        /// </summary>
        [XmlAttribute("RelativeTargetFilePath")]
        public string RelativeTargetFilePath { get; set; }
        
        /// <summary>
        /// Path to the archive file
        /// </summary>
        [XmlAttribute("TargetCabFilePath")]
        [BaseDirectory(Type = BaseDirectoryType.OutputDirectory)]
        public string TargetCabFilePath { get; set; }

        public override string GetTargetFilePath() => Path.Combine(TargetCabFilePath, RelativeTargetFilePath);
    }
}