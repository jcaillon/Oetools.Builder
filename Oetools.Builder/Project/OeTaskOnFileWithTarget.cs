using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    
   
    public abstract class OeTaskOnFileWithTarget : OeTaskOnFile, ITaskOnFileWithTarget {
            
        [XmlAttribute("TargetFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetFilePath { get; set; }
        
        [XmlAttribute("TargetDirectory")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetDirectory { get; set; }

        protected string GetTarget() => TargetFilePath ?? TargetDirectory;

        protected bool AppendFileNameToTargetPath => string.IsNullOrEmpty(TargetFilePath);
        
        public override void Validate() {
            base.Validate();
            if (!string.IsNullOrEmpty(TargetFilePath) && !string.IsNullOrEmpty(TargetDirectory)) {
                throw new TaskValidationException(this, $"{GetType().GetXmlName(nameof(TargetFilePath))} and {GetType().GetXmlName(nameof(TargetDirectory))} can't be both defined for a given task, choose only one");
            }
            ValidateTargets(GetTarget().Split(';'));
        }
        
        private void ValidateTargets(List<string> targets) {
            targets.ForEach(s => {
                try {
                    BuilderUtilities.ValidateTargetPath(s);
                } catch (Exception e) {
                    throw new TaskValidationException(this, $"Invalid target path, reason : {e.Message}, please check the following string : {s.PrettyQuote()}");
                }
            });
        }

        public virtual List<string> GetFileTargets(OeFile file, string outputDirectory = null) {
            var output = new List<string>();
            var sourceFileDirectory = Path.GetDirectoryName(file.SourcePath);

            foreach (var regex in GetIncludeRegex()) {
                var match = regex.Match(file.SourcePath);
                if (!match.Success) {
                    continue;
                }

                foreach (var singleTarget in GetTarget().Split(';')) {
                    var target = singleTarget.ReplacePlaceHolders(s => {
                        if (s.Equals("FILE_SOURCE_DIRECTORY")) {
                            return sourceFileDirectory;
                        }
                        if (match.Groups[s].Success) {
                            return match.Groups[s].Value;
                        }
                        return string.Empty;
                    });

                    // if we target a directory, append the filename
                    if (AppendFileNameToTargetPath) {
                        target = Path.Combine(target, Path.GetFileName(file.SourcePath));
                    } else {
                        target = target.TrimDirectorySeparator();
                    }

                    // take care of relative target path
                    if (!string.IsNullOrEmpty(outputDirectory) && Utils.IsPathRooted(target)) {
                        target = Path.Combine(outputDirectory, target);
                    }
                
                    output.Add(target);
                }
            }

            return output;
        }

    }
}