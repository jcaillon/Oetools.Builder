using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    public abstract class OeTaskOnFileWithTarget : OeTaskOnFile, ITaskOnFileWithTarget {
            
        public List<string> GetFileTargets(OeFile file) {
            var output = new List<string>();

            foreach (var regex in GetIncludeRegex()) {
                var match = regex.Match(file.SourcePath);
                if (!match.Success) {
                    continue;
                }
                var target = Target.ReplacePlaceHolders(s => {
                    if (match.Groups[s].Success) {
                        return match.Groups[s].Value;
                    }
                    return string.Empty;
                });

                if (AppendFileNameToTargetPath) {
                    target = Path.Combine(target, Path.GetFileName(file.SourcePath));
                } else {
                    target = target.TrimEnd(Path.PathSeparator);
                }
                output.Add(target);
            }

            return output;
        }

        public bool AppendFileNameToTargetPath => false;
        
        [XmlAttribute("Target")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string Target { get; set; }
    }
}