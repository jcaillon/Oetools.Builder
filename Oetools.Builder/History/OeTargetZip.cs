using System;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeTargetZip : OeTargetPack {
        
        /// <summary>
        /// Relative path inside the archive
        /// </summary>
        [XmlAttribute("RelativeTargetFilePath")]
        [BaseDirectory(SkipReplace = true)]
        public string RelativeTargetFilePath { get; set; }
        
        /// <summary>
        /// Path to the archive file
        /// </summary>
        [XmlAttribute("TargetZipFilePath")]
        [BaseDirectory(Type = BaseDirectoryType.OutputDirectory)]
        public string TargetZipFilePath { get; set; }

        public override string GetTargetFilePath() => Path.Combine(TargetZipFilePath, RelativeTargetFilePath);
    }
}