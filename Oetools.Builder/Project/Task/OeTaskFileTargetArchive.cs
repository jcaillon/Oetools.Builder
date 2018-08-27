#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTargetArchive.cs) is part of Oetools.Builder.
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
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    public abstract class OeTaskFileTargetArchive : OeTaskFileTarget, IOeTaskFileTargetArchive {
            
        /// <summary>
        /// Relative path inside the archive
        /// </summary>
        [XmlAttribute("RelativeTargetFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string RelativeTargetFilePath { get; set; }
        
        /// <summary>
        /// Relative path inside the archive
        /// </summary>
        [XmlAttribute("RelativeTargetDirectory")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string RelativeTargetDirectory { get; set; }

        /// <summary>
        /// Compression level for this archive task
        /// </summary>
        /// <returns></returns>
        public virtual OeCompressionLevel GetArchivesCompressionLevel() => OeCompressionLevel.None;
        
        /// <summary>
        /// The pack target path for this archive task
        /// </summary>
        /// <returns></returns>
        public virtual string GetTargetArchive() => throw new NotImplementedException();
        
        public virtual string GetTargetArchivePropertyName() => throw new NotImplementedException();

        /// <inheritdoc cref="IOeTaskFile.ExecuteForFiles"/>
        public virtual void ExecuteForFilesTargetArchives(IEnumerable<IOeFileToBuildTargetArchive> files) {
            throw new NotImplementedException();
        }
        
        public override void Validate() {
            if (string.IsNullOrEmpty(RelativeTargetFilePath) && RelativeTargetDirectory == null) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined : {GetType().GetXmlName(nameof(RelativeTargetFilePath))} and/or {GetType().GetXmlName(nameof(RelativeTargetDirectory))}");
            }
            if (string.IsNullOrEmpty(GetTargetArchive())) {
                throw new TaskValidationException(this, $"This task needs the following propertiy to be defined : {GetType().GetXmlName(GetTargetArchivePropertyName())}");
            }
            CheckTargetPath((RelativeTargetFilePath?.Split(';')).UnionHandleNull(RelativeTargetDirectory?.Split(';')));
            CheckTargetPath(GetTargetArchive()?.Split(';'));
            base.Validate();
        }

        protected virtual OeTargetArchive GetNewTargetArchive() => throw new NotImplementedException();
        
        /// <summary>
        /// Returns a collection of archive path -> list of relative targets inside that archive which represents the targets
        /// for this task and for the given <param name="filePath" />
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="baseTargetDirectory"></param>
        /// <returns></returns>
        public List<OeTargetArchive> GetFileTargets(string filePath, string baseTargetDirectory) {
            var output = new List<OeTargetArchive>();
            foreach (var regex in GetIncludeRegex()) {
                
                // find the regexes that included this file
                var match = regex.Match(filePath);
                if (!match.Success) {
                    continue;
                }
                
                foreach (var archivePath in (GetTargetArchive()?.Split(';')).ToNonNullList()) {
                    
                    foreach (var fileTarget in (RelativeTargetFilePath?.Split(';')).ToNonNullList()) {
                        var targetArchive = GetNewTargetArchive();
                        targetArchive.TargetPackFilePath = GetSingleTargetPath(archivePath, false, match, filePath, baseTargetDirectory, false);
                        targetArchive.RelativeTargetFilePath = GetSingleTargetPath(fileTarget, false, match, filePath, null, true);
                        output.Add(targetArchive);
                    }
                    
                    foreach (var directoryTarget in (RelativeTargetDirectory?.Split(';')).ToNonNullList()) {
                        var targetArchive = GetNewTargetArchive();
                        targetArchive.TargetPackFilePath = GetSingleTargetPath(archivePath, false, match, filePath, baseTargetDirectory, false);
                        targetArchive.RelativeTargetFilePath = GetSingleTargetPath(directoryTarget, true, match, filePath, null, true);
                        output.Add(targetArchive);
                    }
                    
                }
                
                // stop after the first include match, if a file was included with several path pattern, we only take the first one
                break;
            }

            return output;
        }

    }
}