using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    public abstract class OeTaskOnFile : OeTask, ITaskExecuteOnFile {
        
        /// <summary>
        /// Validates that the task is correct (correct parameters and can execute)
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public override void ValidateTask() {
            base.ValidateTask();
            if (!string.IsNullOrEmpty(IncludeRegex) && !string.IsNullOrEmpty(Include)) {
                throw new TaskValidationException(this, $"{nameof(Include)} and  {nameof(IncludeRegex)} can't be both defined for a given task, choose only one");
            }
            if (!string.IsNullOrEmpty(ExcludeRegex) && !string.IsNullOrEmpty(Exclude)) {
                throw new TaskValidationException(this, $"{nameof(Exclude)} and  {nameof(ExcludeRegex)} can't be both defined for a given task, choose only one");
            }
            InitTask();
        }

        /// <summary>
        /// Initiate private fields for this task
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        private void InitTask() {
            _includeOriginalStrings = (Include ?? IncludeRegex).Split(';').ToList();
            _excludeOriginalStrings = (Exclude ?? ExcludeRegex).Split(';').ToList();
            
            _includeRegexStrings = !string.IsNullOrEmpty(Include) ? ToWildCards(_includeOriginalStrings) : _includeOriginalStrings;
            _excludeRegexStrings = !string.IsNullOrEmpty(Exclude) ? ToWildCards(_excludeOriginalStrings) : _excludeOriginalStrings;

            _includeRegexes = ToRegexes(_includeRegexStrings);
            _excludeRegexes = ToRegexes(_excludeRegexStrings);
        }

        /// <summary>
        /// Converts a list of regex strings to regex(es)
        /// </summary>
        /// <param name="regexStrings"></param>
        /// <returns></returns>
        /// <exception cref="TaskValidationException"></exception>
        private List<Regex> ToRegexes(List<string> regexStrings) {
            var output = new List<Regex>();
            foreach (var regexString in regexStrings) {
                try {
                    output.Add(new Regex(regexString));
                } catch (Exception e) {
                    throw new TaskValidationException(this, $"Invalid regex expression {regexString.PrettyQuote()}, reason : {e.Message}");
                }
            }
            return output;
        }

        /// <summary>
        /// Convert a list of wildward string to regex strings
        /// </summary>
        /// <param name="originalStrings"></param>
        /// <returns></returns>
        /// <exception cref="TaskValidationException"></exception>
        private List<string> ToWildCards(List<string> originalStrings) {
            originalStrings.ForEach(s => {
                try {
                    Utils.ValidatePathWildCard(s);
                } catch (Exception e) {
                    throw new TaskValidationException(this, $"Invalid path, reason : {e.Message}, please check the following string : {s.PrettyQuote()}");
                }
            });
            return originalStrings.Select(s => s.PathWildCardToRegex()).ToList();
        }

        private List<string> _includeOriginalStrings;
        private List<string> _excludeOriginalStrings;
        private List<string> _includeRegexStrings;
        private List<string> _excludeRegexStrings;
        private List<Regex> _includeRegexes;
        private List<Regex> _excludeRegexes;

        public bool IsFileIncluded(OeFile file) {
            var source = file.SourcePath;
            return _includeRegexes.Any(regex => regex.IsMatch(source));
        }
        
        public bool IsFileExcluded(OeFile file) {
            var source = file.SourcePath;
            return _excludeRegexes.All(regex => !regex.IsMatch(source));
        }

        public virtual void ExecuteForFile(OeFile file) { }

        public List<OeFileBuilt> GetFilesBuilt() => null;
        
        public List<string> GetIncludeRegexStrings() {
            if (_includeRegexStrings == null) {
                InitTask();
            }
            return _includeRegexStrings;
        }

        public List<string> GetExcludeRegexStrings() {
            if (_excludeRegexStrings == null) {
                InitTask();
            }

            return _excludeRegexStrings;
        }

        public List<string> GetIncludeOriginalStrings() {
            if (_includeOriginalStrings == null) {
                InitTask();
            }

            return _includeOriginalStrings;
        }

        public List<string> GetExcludeOriginalStrings() {
            if (_excludeOriginalStrings == null) {
                InitTask();
            }

            return _excludeOriginalStrings;
        }
        
        protected List<Regex> GetIncludeRegex() {
            if (_includeRegexes == null) {
                InitTask();
            }
            return _includeRegexes;
        }

        /// <summary>
        /// Separate different path with ;
        /// </summary>
        /// <remarks>
        /// if a file matches several include patterns (separated with ;) the task will be applied several times to this file
        /// </remarks>
        [XmlAttribute("Include")]
        public string Include { get; set; }
            
        /// <summary>
        /// Separate different path with ;
        /// </summary>
        [XmlAttribute("Exclude")]
        public string Exclude { get; set; }

        /// <summary>
        /// Separate different path with ;
        /// </summary>
        [XmlAttribute("IncludeRgx")]
        public string IncludeRegex { get; set; }
            
        /// <summary>
        /// Separate different path with ;
        /// </summary>
        [XmlAttribute("ExcludeRgx")]
        public string ExcludeRegex { get; set; }
    }
}