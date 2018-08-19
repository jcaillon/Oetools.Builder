using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    
    public abstract class OeTask : ITask {
        
        protected ILogger _log { get; set; }
        
        /// <summary>
        /// Validates that the task is correct (correct parameters and can execute)
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public virtual void Validate() { }
        
        [XmlAttribute("Label")]
        public string Label { get; set; }

        public override string ToString() {
            return $"{(string.IsNullOrEmpty(Label) ? "Unnamed task" : $"Task {Label}")} of type {GetType().GetXmlName()}";
        }
        public void SetLog(ILogger log) {
            _log = log;
        }
    }
}