#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFilter.cs) is part of Oetools.Builder.
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
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
 
    [Serializable]
    public abstract class AOeTaskFilter : AOeTask, IOeTaskFilter {

        /// <summary>
        /// A path pattern that describes paths that should be processed by this task.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Several pattern can be used, separate them with a semi-colon (i.e. ;).
        /// The following symbols can be used in patterns:
        /// 
        /// - ** will match any char any number of time (corresponds to a regex greedy match)
        /// - * will match only non path separators any time (can be used to match any file name)
        /// - ? matches non path separators exactly 1 time
        /// - (( will start capturing characters (equivalent to regex capturing parenthesis)
        /// - )) will stop capturing characters
        /// - || will corresponds to a "or" in a captured context (equivalent to | in regex)
        /// 
        /// Fyi, internally, each pattern is turned into a valid regular expression.
        ///
        /// If a file is matched by several patterns, only the first one will be used.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>
        /// ** will match any path.
        /// ((C:\**))((*.txt)) will match any txt file in C:\ and also captures the full path (group 1) as well as the file name (group 2).
        /// </para>
        /// </example>
        [XmlAttribute(AttributeName = "Include")]
        public string Include {
            get => _include;
            set {
                _includeRegexes = null; 
                _include = value;
            }
        }

        /// <summary>
        /// A regular expression path pattern that describes paths that should be processed by this task.
        /// </summary>
        [XmlAttribute(AttributeName = "IncludeRegex")]
        public virtual string IncludeRegex {
            get => _includeRegex;
            set {
                _includeRegexes = null; 
                _includeRegex = value;
            }
        }
        
        /// <summary>
        /// A path pattern that describes paths that should be excluded from being processed by this task.
        /// </summary>
        /// <inheritdoc cref="Include"/>
        [XmlAttribute(AttributeName = "Exclude")]
        public string Exclude {
            get => _exclude;
            set {
                _excludeRegexes = null; 
                _exclude = value;
            }
        }

        /// <summary>
        /// A regular expression path pattern that describes paths that should be excluded from being processed by this task.
        /// </summary>
        [XmlAttribute(AttributeName = "ExcludeRegex")]
        public string ExcludeRegex {
            get => _excludeRegex;
            set {
                _excludeRegexes = null; 
                _excludeRegex = value;
            }
        }

        private List<Regex> _excludeRegexes;
        private List<Regex> _includeRegexes;
        private string _exclude;
        private string _include;
        private string _includeRegex;
        private string _excludeRegex;
        private string _fileExtensionsFilter;
        
        public List<string> GetRegexIncludeStrings() => (Include?.Split(';').Select(s => s.PathWildCardToRegex())).UnionHandleNull(IncludeRegex?.Split(';'));
        public List<string> GetRegexExcludeStrings() => (Exclude?.Split(';').Select(s => s.PathWildCardToRegex())).UnionHandleNull(ExcludeRegex?.Split(';'));

        public List<string> GetIncludeStrings() => (Include?.Split(';')).ToNonNullList();
        public List<string> GetExcludeStrings() => (Exclude?.Split(';')).ToNonNullList();

        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            CheckWildCards(GetIncludeStrings());
            CheckWildCards(GetExcludeStrings());
            InitRegex();
        }

        /// <inheritdoc cref="IOeTaskFilter.FilterFiles"/>
        public PathList<IOeFile> FilterFiles(PathList<IOeFile> originalListOfPaths) {
            return originalListOfPaths?.CopyWhere(f => IsPathPassingFilter(f.Path));
        }

        /// <inheritdoc cref="IOeTaskFilter.FilterDirectories"/>
        public PathList<IOeDirectory> FilterDirectories(PathList<IOeDirectory> originalListOfPaths) {
            return originalListOfPaths?.CopyWhere(f => IsPathPassingFilter(f.Path));
        }
        
        /// <inheritdoc cref="IOeTaskFilter.IsPathPassingFilter"/>
        public bool IsPathPassingFilter(string path) {
            return IsPathIncluded(path) && !IsPathExcluded(path);
        }
        
        /// <inheritdoc cref="IOeTaskFilter.IsPathIncluded"/>
        public bool IsPathIncluded(string path) {
            if (!string.IsNullOrEmpty(_fileExtensionsFilter)) {
                if (!path.TestFileNameAgainstListOfPatterns(_fileExtensionsFilter)) {
                    return false;
                }
            }
            var includeRegexes = GetIncludeRegex();
            return includeRegexes.Count == 0 || includeRegexes.Any(regex => regex.IsMatch(path));
        }
        
        /// <inheritdoc cref="IOeTaskFilter.IsPathExcluded"/>
        public bool IsPathExcluded(string path) {
            return GetExcludeRegex().Any(regex => regex.IsMatch(path));
        }

        /// <inheritdoc cref="IOeTaskFilter.SetFileExtensionFilter"/>
        public void SetFileExtensionFilter(string fileExtensionFiler) {
            if (string.IsNullOrEmpty(_fileExtensionsFilter)) {
                _fileExtensionsFilter = fileExtensionFiler;
            }
        }
        
        protected List<Regex> GetExcludeRegex() {
            if (_excludeRegexes == null) {
                InitRegex();
            }
            return _excludeRegexes;
        }
        
        protected List<Regex> GetIncludeRegex() {
            if (_includeRegexes == null) {
                InitRegex();
            }
            return _includeRegexes;
        }
        
        private void InitRegex() {
            _excludeRegexes = ToRegexes(GetRegexExcludeStrings());
            _includeRegexes = ToRegexes(GetRegexIncludeStrings());
        }
        
        /// <summary>
        /// Converts a list of regex strings to regex(es)
        /// </summary>
        /// <param name="regexStrings"></param>
        /// <returns></returns>
        /// <exception cref="TaskValidationException"></exception>
        private List<Regex> ToRegexes(List<string> regexStrings) {
            if (regexStrings == null) {
                return new List<Regex>();
            }
            var output = new List<Regex>();
            var i = 0;
            foreach (var regexString in regexStrings) {
                try {
                    output.Add(new Regex(regexString));
                } catch (Exception e) {
                    var ex = new FilterValidationException($"Invalid filter regex expression {regexString.PrettyQuote()}. {e.Message}", e) {
                        FilterNumber = i
                    };
                    throw ex;
                }
                i++;
            }
            return output;
        }

        /// <summary>
        /// Check that a list of string are valid wild card path
        /// </summary>
        /// <param name="originalStrings"></param>
        /// <returns></returns>
        /// <exception cref="TaskValidationException"></exception>
        private void CheckWildCards(IEnumerable<string> originalStrings) {
            if (originalStrings == null) {
                return;
            }
            var i = 0;
            foreach (var originalString in originalStrings) {
                try {
                    Utils.ValidatePathWildCard(originalString);
                } catch (Exception e) {
                    var ex = new FilterValidationException($"Invalid path expression {originalString.PrettyQuote()}. {e.Message}", e) {
                        FilterNumber = i
                    };
                    throw ex;
                }
                i++;
            }
        }

        /// <summary>
        /// Returns a collection of archive path -> list of relative targets inside that archive which represents the targets
        /// for this task and for the given <paramref name="filePath" />.
        /// </summary>
        /// <param name="filePath">The file path for which to find the targets.</param>
        /// <param name="baseTargetDirectory">Can be null.</param>
        /// <param name="targetArchive"></param>
        /// <param name="relativeTargetFilePath"></param>
        /// <param name="relativeTargetDirectory"></param>
        /// <param name="getNewTarget"></param>
        /// <returns></returns>
        protected List<AOeTarget> GetTargets(string filePath, string baseTargetDirectory, string targetArchive, string relativeTargetFilePath, string relativeTargetDirectory, Func<AOeTarget> getNewTarget) {
            var output = new List<AOeTarget>();
            foreach (var regex in GetIncludeRegex()) {
                
                // find the regexes that included this file
                var match = regex.Match(filePath);
                if (!match.Success) {
                    continue;
                }
                
                foreach (var archivePath in targetArchive?.Split(';') ?? new string[] { null }) {

                    bool hasArchivePath = !string.IsNullOrEmpty(archivePath);
                    string targetArchiveFilePath = hasArchivePath ? GetSingleTargetPath(archivePath, false, match, filePath, baseTargetDirectory, false) : null;
                    
                    foreach (var fileTarget in (relativeTargetFilePath?.Split(';')).ToNonNullList()) {
                        var target = getNewTarget();
                        target.ArchiveFilePath = targetArchiveFilePath;
                        target.FilePathInArchive = GetSingleTargetPath(fileTarget, false, match, filePath, hasArchivePath ? null : baseTargetDirectory, hasArchivePath);
                        output.Add(target);
                    }
                    
                    foreach (var directoryTarget in (relativeTargetDirectory?.Split(';')).ToNonNullList()) {
                        var target = getNewTarget();
                        target.ArchiveFilePath = targetArchiveFilePath;
                        target.FilePathInArchive = GetSingleTargetPath(directoryTarget, true, match, filePath, hasArchivePath ? null : baseTargetDirectory, hasArchivePath);
                        output.Add(target);
                    }
                    
                }
                
                // stop after the first include match, if a file was included with several path pattern, we only take the first one
                break;
            }

            return output;
        }
        
        /// <summary>
        /// Computes a single target file for the given <paramref name="sourcePath"/>.
        /// </summary>
        /// <param name="targetPathWithPlaceholders">target path that can contain placeholders.</param>
        /// <param name="isDirectoryPath">Is the target path a directory.</param>
        /// <param name="match">The match result of the source path with the include regex.</param>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="baseTargetDirectory">The base target directory</param>
        /// <param name="mustBeRelativePath">Indicates if the resulting target should be a relative path or not.</param>
        /// <returns></returns>
        /// <exception cref="TaskExecutionException"></exception>
        private string GetSingleTargetPath(string targetPathWithPlaceholders, bool isDirectoryPath, Match match, string sourcePath, string baseTargetDirectory, bool mustBeRelativePath) {
            var sourceFileDirectory = Path.GetDirectoryName(sourcePath);
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
                target = Path.Combine(target, Path.GetFileName(sourcePath ?? ""));
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
                    throw new TaskExecutionException(this, $"This task is not allowed to target a relative path because no base target directory is defined for this task, the error occured for : {targetPathWithPlaceholders.PrettyQuote()}.");
                }
            } else if (mustBeRelativePath) {
                throw new TaskExecutionException(this, $"The following path should resolve to a relative path : {targetPathWithPlaceholders.PrettyQuote()}.");
            }
            
            // get the real full path name of the target
            if (isPathRooted) {
                try {
                    target = Path.GetFullPath(target);
                } catch (Exception e) {
                    throw new TaskExecutionException(this, $"Could not convert the target path to an absolute path, the original path pattern was {targetPathWithPlaceholders.PrettyQuote()}, it was resolved into the target {target.PrettyQuote()} but failed. {e.Message}", e);
                }
            }

            return target;
        }

        /// <summary>
        /// Checks that a list of strings are valid path with placeholders.
        /// </summary>
        /// <param name="originalStrings"></param>
        /// <param name="getPropertyName"></param>
        /// <returns></returns>
        /// <exception cref="TaskValidationException"></exception>
        protected void CheckTargetPath(IEnumerable<string> originalStrings, Func<string> getPropertyName) {
            if (originalStrings == null) {
                return;
            }
            var i = 0;
            foreach (var originalString in originalStrings) {
                try {
                    foreach (char c in Path.GetInvalidPathChars()) {
                        if (originalString.IndexOf(c) >= 0) {
                            throw new Exception($"Illegal character path {c} at column {originalString.IndexOf(c)}.");
                        }
                    }
                    originalString.ValidatePlaceHolders();
                } catch (Exception e) {
                    var ex = new TargetValidationException(this, $"Property {getPropertyName?.Invoke() ?? "?"}, invalid path expression {originalString.PrettyQuote()}. {e.Message}", e) {
                        TargetNumber = i
                    };
                    throw ex;
                }
                i++;
            }
        }
    }
}