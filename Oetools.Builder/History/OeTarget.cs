﻿using System.Xml.Serialization;

namespace Oetools.Builder.History {
    public abstract class OeTarget {
        /// <summary>
        /// Relative target path (relative to the target directory)
        /// </summary>
        [XmlAttribute(AttributeName = "TargetPath")]
        public string TargetPath { get; set; }
    }
}