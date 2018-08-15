using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeBuildStepCompile : OeBuildStep {
                
        [XmlArray("Tasks")]
        [XmlArrayItem("Execute", typeof(OeTaskExec))]
        [XmlArrayItem("Compile", typeof(OeTaskCompile))]
        [XmlArrayItem("CompileInProlib", typeof(OeTaskCompileProlib))]
        [XmlArrayItem("CompileInZip", typeof(OeTaskCompileZip))]
        [XmlArrayItem("CompileInCab", typeof(OeTaskCompileCab))]
        [XmlArrayItem("CompileUploadFtp", typeof(OeTaskCompileUploadFtp))]
        [XmlArrayItem("Copy", typeof(OeTaskCopy))]
        [XmlArrayItem("Prolib", typeof(OeTaskProlib))]
        [XmlArrayItem("Zip", typeof(OeTaskZip))]
        [XmlArrayItem("Cab", typeof(OeTaskCab))]
        [XmlArrayItem("UploadFtp", typeof(OeTaskFtp))]
        public override List<OeTask> Tasks { get; set; }
    }
}