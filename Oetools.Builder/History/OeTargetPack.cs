﻿using System.Xml.Serialization;

namespace Oetools.Builder.History {
    public abstract class OeTargetPack : OeTarget {
        /// <summary>
        /// Relative path of the pack in which this file is deployed (if any)
        /// </summary>
        [XmlAttribute(AttributeName = "TargetPack")]
        public string TargetPack { get; set; }
    }
}