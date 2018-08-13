using System;
using System.Xml.Serialization;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeWebclientPackage {

        [XmlElement(ElementName = "VendorName")]
        public string VendorName { get; set; }

        [XmlElement(ElementName = "ApplicationName")]
        public string ApplicationName { get; set; }
        
        /// <summary>
        /// Prowcapp version, automatically computed by this tool
        /// </summary>
        [XmlElement(ElementName = "WebclientProwcappVersion")]
        public int WebclientProwcappVersion { get; set; }
    }
}