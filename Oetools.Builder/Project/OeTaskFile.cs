﻿#region header
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
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
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

        public void ValidateCanIncludeFiles() {
            if (string.IsNullOrEmpty(Include)) {
                throw new TaskValidationException(this, $"This task needs to have the property {GetType().GetXmlName(nameof(Include))} defined or it can not be applied on any file");
            }
            if (!string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"The property {GetType().GetXmlName(nameof(IncludeRegex))} is not allowed for this task because it would not allow to find files to include (it would require to list the entire content of all the discs on this computer to match this regular expression), use the property {GetType().GetXmlName(nameof(Include))} instead");
            }
        }
        
        /// <summary>
        /// Given the inclusion wildcard paths and exclusion patterns, returns a list of files on which to apply this task
        /// </summary>
        /// <returns></returns>
        public List<OeFile> GetIncludedFiles() {
            var output = new List<OeFile>();
            var i = 0;
            foreach (var path in GetIncludeStrings()) {
                if (File.Exists(path)) {
                    // the include directly designate a file
                    if (!IsFileExcluded(path)) {
                        output.Add(new OeFile { SourceFilePath = Path.GetFullPath(path) });
                    }
                } else {
                    // the include is a wildcard path, we try to get the "root" folder to list to get all the files
                    var validDir = Utils.GetLongestValidDirectory(path);
                    if (!string.IsNullOrEmpty(validDir)) {
                        Log?.Info($"Listing directory : {validDir.PrettyQuote()}");
                        output.AddRange(new SourceFilesLister(validDir, CancelSource) { SourcePathFilter = this }
                            .GetFileList()
                            .Where(f => GetIncludeRegex()[i].IsMatch(f.SourceFilePath))
                        );
                    } else {
                        AddExecutionWarning(new TaskExecutionException($"The property {GetType().GetXmlName(nameof(Include))} part {i} does not designate a file (e.g. /dir/file.ext) nor does it allow to find a base directory to list (e.g. /dir/**), the path in error is : {path}"));
                    }
                }
                i++;
            }

            // make sure to return unique files
            return output.DistinctBy(file => file.SourceFilePath, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        
        public void ExecuteForFiles(IEnumerable<IOeFileToBuildTargetFile> files) {
            try {
                ExecuteForFilesInternal(files);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception e) {
                AddExecutionError(new TaskExecutionException($"Unexpected error in task {ToString().PrettyQuote()} : {e.Message}", e));
            }
        }

        protected virtual void ExecuteForFilesInternal(IEnumerable<IOeFileToBuildTargetFile> files) {
            throw new NotImplementedException();
        }
        
        public List<OeFileBuilt> GetFilesBuilt() => _filesBuilt;
        
    }
}