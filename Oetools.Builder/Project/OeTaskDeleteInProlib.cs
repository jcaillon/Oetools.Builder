using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeTaskDeleteInProlib : OeTaskOnFile {
            
        /// <summary>
        /// The relative file path pattern to delete inside the matched prolib file
        /// </summary>
        [XmlAttribute("RelativeFilePatternToDelete")]
        public string RelativeFilePatternToDelete { get; set; }
    }
}