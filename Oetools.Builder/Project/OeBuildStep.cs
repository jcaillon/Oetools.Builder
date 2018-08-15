using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeBuildStep {
                
        [XmlAttribute("Label")]
        [ReplaceVariables(SkipReplace = true)]
        public string Label { get; set; }
                
        [XmlArray("Tasks")]
        [XmlArrayItem("Execute", typeof(OeTaskExec))]
        [XmlArrayItem("Copy", typeof(OeTaskCopy))]
        [XmlArrayItem("Move", typeof(OeTaskMove))]
        [XmlArrayItem("RemoveDir", typeof(OeTaskRemoveDir))]
        [XmlArrayItem("Delete", typeof(OeTaskDelete))]
        [XmlArrayItem("DeleteInProlib", typeof(OeTaskDeleteInProlib))]
        [XmlArrayItem("Prolib", typeof(OeTaskProlib))]
        [XmlArrayItem("Zip", typeof(OeTaskZip))]
        [XmlArrayItem("Cab", typeof(OeTaskCab))]
        [XmlArrayItem("UploadFtp", typeof(OeTaskFtp))]
        [XmlArrayItem("Webclient", typeof(OeTaskWebclient))]
        public virtual List<OeTask> Tasks { get; set; }

        /// <summary>
        /// Validate tasks in this step
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public void Validate() {
            var i = 0;
            foreach (var task in Tasks) {
                try {
                    if (string.IsNullOrEmpty(task.Label)) {
                        task.Label = $"Task {i}";
                    }
                    task.Validate();
                } catch (Exception e) {
                    var et = e as TaskValidationException ?? new TaskValidationException(task, "Unexpected exception validating a task", e);
                    et.TaskNumber = i;
                    et.StepName = Label;
                    throw et;
                }
                i++;
            }
        }
    }
}