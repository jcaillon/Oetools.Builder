#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFile.cs) is part of Oetools.Builder.
// 
// Oetools.Builder is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {

    public abstract class OeTaskFile : OeTaskFilter, IOeTaskFile {

        protected List<OeFileBuilt> _filesBuilt;
        
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

        public virtual void ExecuteForFiles(IEnumerable<IOeFileToBuildTargetFile> file) {
            throw new NotImplementedException();
        }

        public List<OeFileBuilt> GetFilesBuilt() => _filesBuilt;
        
    }
}