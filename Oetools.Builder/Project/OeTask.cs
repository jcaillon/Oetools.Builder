using System.Xml.Serialization;
using Oetools.Builder.Exceptions;

namespace Oetools.Builder.Project {
    public abstract class OeTask {
        
        /// <summary>
        /// Validates that the task is correct (correct parameters and can execute)
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public virtual void ValidateTask() { }
        
        [XmlAttribute("Name")]
        public string Name { get; set; }
        
    }
}