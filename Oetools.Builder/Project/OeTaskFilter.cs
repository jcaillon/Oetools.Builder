using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeTaskFilter : OeTask {
        
        /// <summary>
        /// ; are authorized
        /// </summary>
        [XmlAttribute(AttributeName = "Exclude")]
        public string Exclude {
            get => _exclude;
            set {
                _excludeRegexes = null; 
                _exclude = value;
            }
        }

        /// <summary>
        /// Separate different path with ;
        /// </summary>
        [XmlAttribute(AttributeName = "Include")]
        public string Include {
            get => _include;
            set {
                _includeRegexes = null; 
                _include = value;
            }
        }

        /// <summary>
        /// Separate different path with ;
        /// </summary>
        [XmlAttribute(AttributeName = "IncludeRegex")]
        public string IncludeRegex {
            get => _includeRegex;
            set {
                _includeRegexes = null; 
                _includeRegex = value;
            }
        }

        /// <summary>
        /// Separate different path with ;
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
        
        public List<string> GetRegexIncludeStrings() => (Include?.Split(';').Select(s => s.PathWildCardToRegex())).Union2(IncludeRegex?.Split(';'));
        public List<string> GetRegexExcludeStrings() => (Exclude?.Split(';').Select(s => s.PathWildCardToRegex())).Union2(ExcludeRegex?.Split(';'));

        public List<string> GetIncludeStrings() => (Include?.Split(';')).ToNonNullList();
        public List<string> GetExcludeStrings() => (Exclude?.Split(';')).ToNonNullList();

        /// <summary>
        /// Validates that the filter is correct and usable
        /// </summary>
        /// <exception cref="FilterValidationException"></exception>
        public override void Validate() {
            CheckWildCards(GetIncludeStrings());
            CheckWildCards(GetExcludeStrings());
            InitRegex();
        }
        
        /// <summary>
        /// Returns true if the given file passes this filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsFilePassingFilter(string filePath) {
            return IsFileIncluded(filePath) && !IsFileExcluded(filePath);
        }
        
        /// <summary>
        /// Returns true of the given path is included with this filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsFileIncluded(string filePath) {
            var includeRegexes = GetIncludeRegex();
            return includeRegexes.Count == 0 || includeRegexes.Any(regex => regex.IsMatch(filePath));
        }
        
        /// <summary>
        /// Returns true of the given path is excluded with this filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsFileExcluded(string filePath) {
            return GetExcludeRegex().Any(regex => regex.IsMatch(filePath));
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
                    var ex = new FilterValidationException(this, $"Invalid filter regex expression {regexString.PrettyQuote()}, reason : {e.Message}", e) {
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
                    var ex = new FilterValidationException(this, $"Invalid path expression {originalString.PrettyQuote()}, reason : {e.Message}", e) {
                        FilterNumber = i
                    };
                    throw ex;
                }
                i++;
            }
        }
    }
}