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
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    public abstract class OeTaskFileTarget : OeTaskFile {
        
        protected string GetSingleTargetPath(string targetPath, bool isDirectoryPath, Match match, string sourceFilePath, string outputDirectory) {
            var sourceFileDirectory = Path.GetDirectoryName(sourceFilePath);
            var target = targetPath.ReplacePlaceHolders(s => {
                if (s.Equals(OeBuilderConstants.OeVarNameFileSourceDirectory)) {
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

            // take care of relative target path
            if (!string.IsNullOrEmpty(outputDirectory) && !Utils.IsPathRooted(target)) {
                target = Path.Combine(outputDirectory, target);
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
                    var ex = new TargetValidationException(this, $"Invalid path expression {originalString.PrettyQuote()}, reason : {e.Message}", e) {
                        TargetNumber = i
                    };
                    throw ex;
                }
                i++;
            }
        }
        
    }
}