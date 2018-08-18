using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    
    [Serializable]
    [XmlRoot("Exec")]
    public class OeTaskExec : OeTask, ITaskExecute {
            
        public void Execute() => throw new NotImplementedException();
        
        [XmlElement("ExecuablePath")]
        public string ExecuablePath { get; set; }
            
        /// <summary>
        /// (you can use task variables in this string)
        /// </summary>
        [XmlElement("Parameters")]
        public string Parameters { get; set; }
            
        [XmlElement(ElementName = "HiddenExecution")]
        public bool? HiddenExecution { get; set; }
            
        /// <summary>
        /// With this option, the task will not fail if the exit code is different of 0
        /// </summary>
        [XmlElement(ElementName = "IgnoreExitCode")]
        public bool? IgnoreExitCode { get; set; }
            
        /// <summary>
        /// (default to output directory)
        /// </summary>
        [XmlElement("WorkingDirectory")]
        public string WorkingDirectory { get; set; }

    }
}