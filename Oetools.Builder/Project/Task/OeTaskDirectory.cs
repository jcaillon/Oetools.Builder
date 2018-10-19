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
using System.IO;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {

    public abstract class OeTaskDirectory : OeTaskFilter, IOeTaskDirectory {
        
        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            if (string.IsNullOrEmpty(Include) && string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined or it will not do anything : {GetType().GetXmlName(nameof(Include))} and/or {GetType().GetXmlName(nameof(IncludeRegex))}");
            }
            base.Validate();
        }

        /// <inheritdoc cref="IOeTaskDirectory.ValidateCanIncludeDirectories"/>
        public void ValidateCanIncludeDirectories() {
            if (string.IsNullOrEmpty(Include)) {
                throw new TaskValidationException(this, $"This task needs to have the property {GetType().GetXmlName(nameof(Include))} defined or it can not be applied on any file");
            }
            if (!string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"The property {GetType().GetXmlName(nameof(IncludeRegex))} is not allowed for this task because it would not allow to find files to include (it would require to list the entire content of all the discs on this computer to match this regular expression), use the property {GetType().GetXmlName(nameof(Include))} instead");
            }
        }
        
        /// <inheritdoc cref="IOeTaskFile.GetIncludedFiles"/>
        public PathList<OeDirectory> GetIncludedDirectories() {
            throw new NotImplementedException();
            /*
            var output = new FileList<OeFile>();
            var i = 0;
            foreach (var path in GetIncludeStrings()) {
                if (File.Exists(path)) {
                    // the include directly designate a file
                    if (!IsFileExcluded(path)) {
                        output.TryAdd(new OeFile { FilePath = Path.GetFullPath(path) });
                    }
                } else {
                    // the include is a wildcard path, we try to get the "root" folder to list to get all the files
                    var validDir = Utils.GetLongestValidDirectory(path);
                    if (!string.IsNullOrEmpty(validDir)) {
                        Log?.Info($"Listing directory : {validDir.PrettyQuote()}");
                        var regexCorrespondingToPath = GetIncludeRegex()[i];
                        foreach (var file in new SourceFilesLister(validDir, CancelToken) { SourcePathFilter = this, Log = Log } .GetFileList()) {
                            if (regexCorrespondingToPath.IsMatch(file.FilePath)) {
                                output.TryAdd(file);
                            }
                        }
                    } else {
                        AddExecutionWarning(new TaskExecutionException(this, $"The property {GetType().GetXmlName(nameof(Include))} part {i} does not designate a file (e.g. /dir/file.ext) nor does it allow to find a base directory to list (e.g. /dir/**), the path in error is : {path}"));
                    }
                }
                i++;
            }

            return output;
            */
        }
        
        /// <inheritdoc cref="OeTask.ExecuteInternal"/>
        protected sealed override void ExecuteInternal() {
            throw new NotImplementedException();
            //ExecuteForDirectoriesInternal(_filesToBuild);
        }

        /// <summary>
        /// Execute the task for a set of directories.
        /// </summary>
        /// <remarks>
        /// <para>
        /// - The task should create/add a list of files that it builds, list that is returned by <see cref="IOeTaskFileBuilder.GetFilesBuilt"/>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed
        /// - This method can publish warnings using <see cref="OeTask.AddExecutionWarning"/>
        /// </para>
        /// </remarks>
        /// <param name="directories"></param>
        /// <exception cref="TaskExecutionException"></exception>
        protected abstract void ExecuteForDirectoriesInternal(PathList<OeDirectory> directories);

    }
}