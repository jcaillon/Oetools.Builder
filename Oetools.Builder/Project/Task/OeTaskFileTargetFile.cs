#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTargetFile.cs) is part of Oetools.Builder.
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

using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// A base task class for tasks that operates on files and use those files to target other file paths.
    /// </summary>
    public abstract class OeTaskFileTargetFile : OeTaskFileTarget, IOeTaskFileTargetFile {
            
        [XmlAttribute("TargetFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetFilePath { get; set; }
        
        [XmlAttribute("TargetDirectory")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetDirectory { get; set; }

        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            base.Validate();
            if (string.IsNullOrEmpty(TargetFilePath) && TargetDirectory == null) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined : {GetType().GetXmlName(nameof(TargetFilePath))} and/or {GetType().GetXmlName(nameof(TargetDirectory))}");
            }
            CheckTargetPath((TargetFilePath?.Split(';')).UnionHandleNull(TargetDirectory?.Split(';')));
        }
        
        private OeTargetFile GetNewTargetFile() => new OeTargetFileCopy();
        
        /// <summary>
        /// Returns a list of target file path for the corresponding source <paramref name="filePath" />,
        /// relative path are turned into absolute path preprending <paramref name="baseTargetDirectory" />
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="baseTargetDirectory"></param>
        /// <returns></returns>
        public List<OeTargetFile> GetTargetsFiles(string filePath, string baseTargetDirectory) {
            var output = new List<OeTargetFile>();
            foreach (var regex in GetIncludeRegex()) {
                
                // find the regexes that included this file
                var match = regex.Match(filePath);
                if (!match.Success) {
                    continue;
                }
                
                foreach (var fileTarget in (TargetFilePath?.Split(';')).ToNonNullList()) {
                    var targetFile = GetNewTargetFile();
                    targetFile.TargetFilePath = GetSingleTargetPath(fileTarget, false, match, filePath, baseTargetDirectory, false);
                    output.Add(targetFile);
                }
                
                foreach (var directoryTarget in (TargetDirectory?.Split(';')).ToNonNullList()) {
                    var targetFile = GetNewTargetFile();
                    targetFile.TargetFilePath = GetSingleTargetPath(directoryTarget, true, match, filePath, baseTargetDirectory, false);
                    output.Add(targetFile);
                }

                // stop after the first include match, if a file was included with several path pattern, we only take the first one
                break;

            }
            return output;
        }

        /// <inheritdoc cref="OeTaskFile.ExecuteForFilesInternal"/>
        protected sealed override void ExecuteForFilesInternal(PathList<OeFile> paths) { }
        
        /// <inheritdoc cref="IOeTaskFileTargetFile.ExecuteForFilesTargetFiles"/>
        public abstract void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files);

    }
}