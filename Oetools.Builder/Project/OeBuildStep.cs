using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Project {
    
    public abstract class OeBuildStep {
                
        [XmlAttribute("Label")]
        public string Label { get; set; }
        
        [XmlIgnore]
        internal int Id { get; set; }

        public virtual List<AOeTask> GetTaskList() => null;

        /// <summary>
        /// Validate tasks in this step
        /// </summary>
        /// <param name="buildFromIncludeList">should the task also be validated with <see cref="IOeTaskFile.ValidateCanGetFilesToBuildFromIncludes"/></param>
        /// <exception cref="BuildStepException"></exception>
        public void Validate(bool buildFromIncludeList) {
            var tasks = GetTaskList();
            if (tasks == null) {
                return;
            }
            foreach (var task in tasks) {
                try {
                    task.Validate();
                    if (buildFromIncludeList) {
                        if (task is IOeTaskFile taskFile) {
                            taskFile.ValidateCanGetFilesToBuildFromIncludes();
                        }
                        if (task is IOeTaskDirectory taskDirectory) {
                            taskDirectory.ValidateCanGetDirectoriesToBuildFromIncludes();
                        }
                    }
                } catch (Exception e) {
                    throw new BuildStepException(this, e.Message, e);
                }
            }
        }
        
        /// <summary>
        /// Give each task a unique number to identify it
        /// </summary>
        internal virtual void InitIds() { }

        protected void InitIds(List<AOeTask> tasks) {
            if (tasks != null) {
                var i = 0;
                foreach (var task in tasks.Where(v => v != null)) {
                    task.Id = i;
                    i++;
                }
            }
        }
        
        public override string ToString() => $"Step [{Id}]{(string.IsNullOrEmpty(Label) ? "" : $" {Label}")}";
    }
}