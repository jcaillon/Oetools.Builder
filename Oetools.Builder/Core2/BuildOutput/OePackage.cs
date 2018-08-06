using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Core.Execution {
    [Serializable]
    public class OePackage {

        /// <summary>
        /// Prowcapp version, automatically computed by this tool
        /// </summary>
        [XmlElement(ElementName = "WebclientProwcappVersion")]
        public int WebclientProwcappVersion { get; set; }
    }
}