using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    
    [Serializable]
    [XmlRoot("AutomatedTasks")]
    public class OeAutomatedTasks {
        
        [XmlAttribute("Label")]
        public string Label { get; set; }
        
        [XmlElement(ElementName = "ArchivesCompressionLevel")]
        public OeCompressionLevel ArchivesCompressionLevel { get; set; }
        
        [XmlArray("Tasks")]
        [XmlArrayItem("Copy", typeof(OeTaskCopy))]
        [XmlArrayItem("Move", typeof(OeTaskMove))]
        [XmlArrayItem("Execute", typeof(OeTaskExec))]
        [XmlArrayItem("RemoveDir", typeof(OeTaskRemoveDir))]
        [XmlArrayItem("Delete", typeof(OeTaskDelete))]
        [XmlArrayItem("DeleteInProlib", typeof(OeTaskDeleteInProlib))]
        [XmlArrayItem("Prolib", typeof(OeTaskProlib))]
        [XmlArrayItem("Zip", typeof(OeTaskZip))]
        [XmlArrayItem("Cab", typeof(OeTaskCab))]
        [XmlArrayItem("UploadFtp", typeof(OeTaskFtp))]
        [XmlArrayItem("Webclient", typeof(OeTaskWebclient))]
        public List<OeTask> Tasks { get; set; }
    }
}