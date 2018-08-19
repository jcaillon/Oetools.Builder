using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    public abstract class OeTaskOnFileWithTargetFiles : OeTaskOnFileWithTarget, ITaskOnFileWithTarget {
            
        [XmlAttribute("TargetFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetFilePath { get; set; }
        
        [XmlAttribute("TargetDirectory")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetDirectory { get; set; }

        public override void Validate() {
            base.Validate();
            if (string.IsNullOrEmpty(TargetFilePath) && string.IsNullOrEmpty(TargetDirectory)) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined : {GetType().GetXmlName(nameof(TargetFilePath))} and/or {GetType().GetXmlName(nameof(TargetDirectory))}");
            }
            CheckTargetPath((TargetFilePath?.Split(';')).Union2(TargetDirectory?.Split(';')));
        }
        
        /// <summary>
        /// Returns a list of target file path for the corresponding source <param name="filePath" />,
        /// relative path are turned into absolute path preprending <param name="outputDirectory" />
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="outputDirectory"></param>
        /// <returns></returns>
        public List<string> GetFileTargets(string filePath, string outputDirectory = null) {
            var output = new List<string>();
            foreach (var regex in GetIncludeRegex()) {
                
                // find the regexes that included this file
                var match = regex.Match(filePath);
                if (!match.Success) {
                    continue;
                }
                foreach (var fileTarget in (TargetFilePath?.Split(';')).ToNonNullList()) {
                    output.Add(GetSingleTargetPath(fileTarget, false, match, filePath, outputDirectory));
                }
                foreach (var directoryTarget in (TargetDirectory?.Split(';')).ToNonNullList()) {
                    output.Add(GetSingleTargetPath(directoryTarget, true, match, filePath, outputDirectory));
                }
            }

            return output;
        }

    }
}