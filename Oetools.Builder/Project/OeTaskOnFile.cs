using System;
using System.Collections.Generic;
using System.IO;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {

    public abstract class OeTaskOnFile : OeTaskFilter, ITaskExecuteOnFile {
        
        public override void Validate() {
            if (string.IsNullOrEmpty(Include) && string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined or it will not do anything : {GetType().GetXmlName(nameof(Include))} and/or {GetType().GetXmlName(nameof(IncludeRegex))}");
            }
            base.Validate();
        }
        
        /// <summary>
        /// Given the inclusion wildcard paths, returns a list of files/folders on which to apply the given task
        /// </summary>
        /// <returns></returns>
        public List<string> GetIncludedPathToList() {
            var output = new List<string>();
            foreach (var includeString in GetIncludeStrings()) {
                if (File.Exists(includeString)) {
                    // the include directly designate a file
                    output.Add(includeString);
                } else {
                    // the include is a wildcard path, we try to get the "root" folder to list to get all the files
                    var validDir = Utils.GetLongestValidDirectory(includeString);
                    if (!string.IsNullOrEmpty(validDir)) {
                        output.Add(validDir);
                    }
                }
            }
            return output;
        }

        public virtual void ExecuteForFile(OeFile file) { }

        public List<OeFileBuilt> GetFilesBuilt() => null;
        
    }
}