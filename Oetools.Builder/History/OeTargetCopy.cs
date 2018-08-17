using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeTargetCopy : OeTarget {
        
        /// <summary>
        /// Target file path
        /// </summary>
        [XmlAttribute(AttributeName = "TargetFilePath")]
        [BaseDirectory(Type = BaseDirectoryType.OutputDirectory)]
        public string TargetFilePath { get; set; }

        public override string GetTargetFilePath() => TargetFilePath;
    }
}