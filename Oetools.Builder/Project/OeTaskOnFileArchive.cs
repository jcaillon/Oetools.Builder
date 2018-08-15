using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    
   
    public abstract class OeTaskOnFileArchive : OeTaskOnFile, ITaskArchive {
            
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
        
        public virtual string GetTargetArchive() => throw new System.NotImplementedException();

        protected string GetTarget() => RelativeTargetFilePath ?? RelativeTargetDirectory;

        protected bool AppendFileNameToTargetPath => string.IsNullOrEmpty(RelativeTargetFilePath);
        
        public override void Validate() {
            base.Validate();
            if (!string.IsNullOrEmpty(RelativeTargetFilePath) && !string.IsNullOrEmpty(RelativeTargetDirectory)) {
                throw new TaskValidationException(this, $"{GetType().GetXmlName(nameof(RelativeTargetFilePath))} and {GetType().GetXmlName(nameof(RelativeTargetDirectory))} can't be both defined for a given task, choose only one");
            }
            ValidateTargets(GetTarget().Split(';'));
            ValidateTargets(GetTargetArchive().Split(';'));
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

        public Dictionary<string, List<string>> GetFileTargets(OeFile file, string outputDirectory = null) {
            var output = new Dictionary<string, List<string>>();
            var sourceFileDirectory = Path.GetDirectoryName(file.SourcePath);

            foreach (var regex in GetIncludeRegex()) {
                var match = regex.Match(file.SourcePath);
                if (!match.Success) {
                    continue;
                }

                foreach (var singleTargetArchive in GetTargetArchive().Split(';')) {
                    var relativePathList = new List<string>();
                    
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

                        relativePathList.Add(target);
                    }

                    if (output.ContainsKey(singleTargetArchive)) {
                        output[singleTargetArchive].AddRange(relativePathList);
                    } else {
                        output.Add(singleTargetArchive, relativePathList);
                    }
                }
            }

            return output;
        }
    }
}