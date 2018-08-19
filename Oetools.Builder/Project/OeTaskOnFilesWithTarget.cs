using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    public abstract class OeTaskOnFilesWithTarget : OeTaskOnFiles {
        
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