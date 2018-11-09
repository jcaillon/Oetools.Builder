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
using System.Text.RegularExpressions;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// A task that operates on files.
    /// </summary>
    public abstract class AOeTaskFile : AOeTaskFilter, IOeTaskFile {
        
        private PathList<OeFile> _pathsToBuild;
        
        public void SetFilesToBuild(PathList<OeFile> pathsToBuild) {
            _pathsToBuild = pathsToBuild;
        }

        public PathList<OeFile> GetFilesToBuild() => _pathsToBuild;

        /// <summary>
        /// Validates that this task as at least one <see cref="AOeTaskFilter.Include"/> defined and
        /// no <see cref="AOeTaskFilter.IncludeRegex"/>
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public void ValidateCanGetFilesToBuildFromIncludes() {
            if (string.IsNullOrEmpty(Include)) {
                throw new TaskValidationException(this, $"This task needs to have the property {GetType().GetXmlName(nameof(Include))} defined or it can not be applied on any file.");
            }
            if (!string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"The property {GetType().GetXmlName(nameof(IncludeRegex))} is not allowed for this task because it would not allow to find files to include (it would require to list the entire content of all the discs on this computer to match this regular expression), use the property {GetType().GetXmlName(nameof(Include))} instead.");
            }
        }
        
        /// <inheritdoc cref="IOeTaskFile.GetFilesToBuildFromIncludes"/>
        public PathList<OeFile> GetFilesToBuildFromIncludes() {
            var output = new PathList<OeFile>();
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
        
        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected sealed override void ExecuteInternal() {
            if (this is IOeTaskCompile thisOeTaskCompile) {
                try {
                    Log?.Debug("Is a compile task.");
                    var compiledFiles = thisOeTaskCompile.GetCompiledFiles();
                    if (compiledFiles == null) {
                        Log?.Debug("Start file compilation.");
                        compiledFiles = OeFilesCompiler.CompileFiles(thisOeTaskCompile.GetProperties(), _pathsToBuild.CopySelect(f => new UoeFileToCompile(f.Path) {
                            FileSize = f.Size
                        }), CancelToken, Log);
                        thisOeTaskCompile.SetCompiledFiles(compiledFiles);
                    }
                    
                    Log?.Debug("Switching original source files for rcode files to build.");
                    _pathsToBuild = OeFilesCompiler.SetRcodeFilesAsSourceInsteadOfSourceFiles(_pathsToBuild, compiledFiles);
                } catch(Exception e) {
                    throw new TaskExecutionException(this, e.Message, e);
                }
            }
            
            if (this is IOeTaskFileWithTargets taskFileWithTargets) {
                taskFileWithTargets.ExecuteForFilesWithTargets(_pathsToBuild);
            } else {
                ExecuteForFilesInternal(_pathsToBuild);
            }
        }
        
        /// <summary>
        /// Execute the task for a set of files.
        /// </summary>
        /// <remarks>
        /// <para>
        /// - The task should create/add a list of files that it builds, list that is returned by <see cref="IOeTaskWithBuiltFiles.GetBuiltFiles"/>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed
        /// - This method can publish warnings using <see cref="AOeTask.AddExecutionWarning"/>
        /// </para>
        /// </remarks>
        /// <param name="paths"></param>
        /// <exception cref="TaskExecutionException"></exception>
        protected abstract void ExecuteForFilesInternal(IEnumerable<IOeFile> paths);
       
    }
}