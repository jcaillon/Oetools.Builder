using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    public abstract class OeTask {
        
        /// <summary>
        /// Validates that the task is correct (correct parameters and can execute)
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public virtual void Validate() { }
        
        [XmlAttribute("Label")]
        public string Label { get; set; }

        public override string ToString() {
            return $"{(string.IsNullOrEmpty(Label) ? "Unnamed task" : $"Task {Label}")} of type {GetType().GetXmlName() ?? GetType().Name}";
        }
    }
}