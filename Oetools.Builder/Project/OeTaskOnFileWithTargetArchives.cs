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

namespace Oetools.Builder.Project {
    
    public abstract class OeTaskOnFileWithTargetArchives : OeTaskOnFileWithTarget, ITaskArchive {
            
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
        
        public override void Validate() {
            if (string.IsNullOrEmpty(RelativeTargetFilePath) && string.IsNullOrEmpty(RelativeTargetDirectory)) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined : {GetType().GetXmlName(nameof(RelativeTargetFilePath))} and/or {GetType().GetXmlName(nameof(RelativeTargetDirectory))}");
            }
            CheckTargetPath((RelativeTargetFilePath?.Split(';')).Union2(RelativeTargetDirectory?.Split(';')));
            CheckTargetPath(GetTargetArchive()?.Split(';'));
            base.Validate();
        }
        
        /// <summary>
        /// Returns a collection of archive path -> list of relative targets inside that archive which represents the targets
        /// for this task and for the given <param name="filePath" />
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="outputDirectory"></param>
        /// <returns></returns>
        public Dictionary<string, List<string>>  GetFileTargets(string filePath, string outputDirectory = null) {
            var output = new Dictionary<string, List<string>>();
            foreach (var regex in GetIncludeRegex()) {
                
                // find the regexes that included this file
                var match = regex.Match(filePath);
                if (!match.Success) {
                    continue;
                }
                
                foreach (var archivePath in (GetTargetArchive()?.Split(';')).ToNonNullList()) {
                    var relativePathList = new List<string>();
                    
                    foreach (var fileTarget in (RelativeTargetFilePath?.Split(';')).ToNonNullList()) {
                        relativePathList.Add(GetSingleTargetPath(fileTarget, false, match, filePath, null));
                    }
                    foreach (var directoryTarget in (RelativeTargetDirectory?.Split(';')).ToNonNullList()) {
                        relativePathList.Add(GetSingleTargetPath(directoryTarget, true, match, filePath, null));
                    }
                    
                    // take care of relative target path
                    string absoluteArchivePath = archivePath;
                    if (!string.IsNullOrEmpty(outputDirectory) && !Utils.IsPathRooted(archivePath)) {
                        absoluteArchivePath = Path.Combine(outputDirectory, archivePath);
                    }

                    if (output.ContainsKey(absoluteArchivePath)) {
                        output[absoluteArchivePath].AddRange(relativePathList);
                    } else {
                        output.Add(absoluteArchivePath, relativePathList);
                    }
                }
            }

            return output;
        }
        
    }
}