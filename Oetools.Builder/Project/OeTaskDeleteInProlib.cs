using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    [XmlRoot("DeleteInProlib")]
    public class OeTaskDeleteInProlib : OeTaskOnFiles {
            
        /// <summary>
        /// The relative file path pattern to delete inside the matched prolib file
        /// </summary>
        [XmlAttribute("RelativeFilePatternToDelete")]
        public string RelativeFilePatternToDelete { get; set; }
    }
}