using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// A build step.
    /// </summary>
    public abstract class AOeBuildStep {
        
        [XmlIgnore]
        internal int Id { get; set; }
                
        /// <summary>
        /// The name of this build step. Purely informative.
        /// </summary>
        [XmlAttribute("Name")]
        public string Name { get; set; }
        
        [XmlIgnore]
        public virtual List<AOeTask> Tasks { get; set; }

        /// <summary>
        /// Should each task also be validated with <see cref="IOeTaskFile.ValidateCanGetFilesToProcessFromIncludes"/>.
        /// </summary>
        /// <returns></returns>
        protected abstract bool AreTasksBuiltFromIncludeList();

        /// <summary>
        /// Validate tasks in this step
        /// </summary>
        /// <exception cref="BuildStepException"></exception>
        public void Validate() {
            if (Tasks == null) {
                return;
            }
            foreach (var task in Tasks) {
                try {
                    task.Validate();
                    if (AreTasksBuiltFromIncludeList()) {
                        if (task is IOeTaskFile taskFile) {
                            taskFile.ValidateCanGetFilesToProcessFromIncludes();
                        }
                        if (task is IOeTaskDirectory taskDirectory) {
                            taskDirectory.ValidateCanGetDirectoriesToProcessFromIncludes();
                        }
                    }
                } catch (Exception e) {
                    throw new BuildStepException(this, e.Message, e);
                }
            }
        }

        public override string ToString() => $"Step [{Id}]{(string.IsNullOrEmpty(Name) ? "" : $" {Name}")}";
    }
}