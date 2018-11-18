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

using System.IO;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// A task that operates on files.
    /// </summary>
    /// <inheritdoc cref="AOeTaskFilter"/>
    public abstract class AOeTaskFile : AOeTaskFilterAttributes, IOeTaskFile {
        
        private PathList<IOeFile> _filesToProcess;
        
        public virtual void SetFilesToProcess(PathList<IOeFile> filesToProcess) {
            _filesToProcess = filesToProcess;
        }

        public virtual PathList<IOeFile> GetFilesToProcess() => _filesToProcess;

        /// <summary>
        /// Validates that this task as at least one <see cref="AOeTaskFilter.Include"/> defined and
        /// no <see cref="AOeTaskFilter.IncludeRegex"/>
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public void ValidateCanGetFilesToProcessFromIncludes() {
            if (string.IsNullOrEmpty(Include)) {
                throw new TaskValidationException(this, $"This task needs to have the property {GetType().GetXmlName(nameof(Include))} defined or it can not be applied on any file.");
            }
            if (!string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"The property {GetType().GetXmlName(nameof(IncludeRegex))} is not allowed for this task because it would not allow to find files to include (it would require to list the entire content of all the discs on this computer to match this regular expression), use the property {GetType().GetXmlName(nameof(Include))} instead.");
            }
        }
        
        /// <inheritdoc cref="IOeTaskFile.GetFilesToProcessFromIncludes"/>
        public PathList<IOeFile> GetFilesToProcessFromIncludes() {
            var output = new PathList<IOeFile>();
            var i = 0;
            foreach (var path in GetIncludeStrings()) {
                if (File.Exists(path)) {
                    // the include directly designate a file
                    if (!IsPathExcluded(path)) {
                        output.TryAdd(new OeFile(Path.GetFullPath(path).ToCleanPath()));
                    }
                } else {
                    // the include is a wildcard path, we try to get the "root" folder to list to get all the files
                    var validDir = Utils.GetLongestValidDirectory(path);
                    if (!string.IsNullOrEmpty(validDir)) {
                        Log?.Info($"Listing directory : {validDir.PrettyQuote()}.");
                        var regexCorrespondingToPath = GetIncludeRegex()[i];
                        foreach (var file in new PathLister(validDir, CancelToken) { Filter = this, Log = Log } .GetFileList()) {
                            if (regexCorrespondingToPath.IsMatch(file.Path)) {
                                output.TryAdd(file);
                            }
                        }
                    } else {
                        AddExecutionWarning(new TaskExecutionException(this, $"The property {GetType().GetXmlName(nameof(Include))} part {i} does not designate a file (e.g. /dir/file.ext) nor does it allow to find a base directory to list (e.g. /dir/**), the path in error is : {path}."));
                    }
                }
                i++;
            }
            return output;
        }
        
       
    }
}