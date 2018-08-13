using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.History;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    public abstract class OeTaskOnFile : OeTask, ITaskExecuteOnFile {

        public bool IsFileIncluded(OeFile file) => true;

        public virtual void ExecuteForFile(OeFile file) { }

        public List<OeFileBuilt> GetFilesBuilt() => null;
        
        public string GetIncludeRegex() {
            return Include.PathWildCardToRegex();
        }

        public string GetExcludeRegex() {
            throw new System.NotImplementedException();
        }

        [XmlAttribute("Include")]
        public string Include { get; set; }
            
        [XmlAttribute("Exclude")]
        public string Exclude { get; set; }
    }
}