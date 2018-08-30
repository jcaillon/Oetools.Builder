#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTarget.cs) is part of Oetools.Builder.
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
    
    public abstract class OeTaskFileTarget : OeTaskFile {
        
        protected string GetSingleTargetPath(string targetPathWithPlaceholders, bool isDirectoryPath, Match match, string sourceFilePath, string baseTargetDirectory, bool mustBeRelativePath) {
            var sourceFileDirectory = Path.GetDirectoryName(sourceFilePath);
            var target = targetPathWithPlaceholders.ReplacePlaceHolders(s => {
                if (s.EqualsCi(OeBuilderConstants.OeVarNameFileSourceDirectory)) {
                    return sourceFileDirectory;
                }
                if (match.Groups[s].Success) {
                    return match.Groups[s].Value;
                }
                return string.Empty;
            });

            // if we target a directory, append the source filename
            if (isDirectoryPath) {
                target = Path.Combine(target, Path.GetFileName(sourceFilePath ?? ""));
            } else {
                target = target.TrimEndDirectorySeparator();
            }

            target = target.ToCleanPath();

            bool isPathRooted = Utils.IsPathRooted(target);
            
            // take care of relative target path
            if (!isPathRooted) {
                if (!string.IsNullOrEmpty(baseTargetDirectory)) {
                    target = Path.Combine(baseTargetDirectory, target);
                    isPathRooted = true;
                } else if (!mustBeRelativePath) {
                    throw new TaskExecutionException(this, $"This task is not allowed to target a relative path because no base target directory is defined for this task, the error occured for : {targetPathWithPlaceholders.PrettyQuote()}");
                }
            } else if (mustBeRelativePath) {
                throw new TaskExecutionException(this, $"The following path should resolve to a relative path : {targetPathWithPlaceholders.PrettyQuote()}");
            }
            
            // get the real full path name of the target
            if (isPathRooted) {
                try {
                    target = Path.GetFullPath(target);
                } catch (Exception e) {
                    throw new TaskExecutionException(this, $"Could not convert the target path to an absolute path, the original path pattern was {targetPathWithPlaceholders.PrettyQuote()}, it was resolved into the target {target.PrettyQuote()} but failed with the exception : {e.Message}", e);
                }
            }

            return target;
        }
        
        /// <summary>
        /// Checks that a list of string are valid path with placeholders
        /// </summary>
        /// <param name="originalStrings"></param>
        /// <returns></returns>
        /// <exception cref="TaskValidationException"></exception>
        protected void CheckTargetPath(IEnumerable<string> originalStrings) {
            if (originalStrings == null) {
                return;
            }
            var i = 0;
            foreach (var originalString in originalStrings) {
                try {
                    foreach (char c in Path.GetInvalidPathChars()) {
                        if (originalString.IndexOf(c) >= 0) {
                            throw new Exception($"Illegal character path {c} at column {originalString.IndexOf(c)}");
                        }
                    }
                    originalString.ValidatePlaceHolders();
                } catch (Exception e) {
                    var ex = new TargetValidationException( $"Invalid path expression {originalString.PrettyQuote()}, reason : {e.Message}", e) {
                        TargetNumber = i
                    };
                    throw ex;
                }
                i++;
            }
        }
        
        private FileList<UoeCompiledFile> CompiledFiles { get; set; }
        public void SetCompiledFiles(FileList<UoeCompiledFile> compiledFile) => CompiledFiles = compiledFile;
        public FileList<UoeCompiledFile> GetCompiledFiles() => CompiledFiles;
        
        protected sealed override void ExecuteForFilesInternal(FileList<OeFile> files) {
            base.ExecuteForFilesInternal(files);
        }
    }
}