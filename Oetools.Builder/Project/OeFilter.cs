using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeFilter {
        
        /// <summary>
        /// ; are authorized
        /// </summary>
        [XmlAttribute(AttributeName = "Exclude")]
        public string Exclude { get; set; }
        
        public virtual IEnumerable<string> GetRegexStrings() => Exclude.Split(';').Select(s => s.PathWildCardToRegex());
        
        /// <summary>
        /// Validates that the filter is correct and usable
        /// </summary>
        /// <exception cref="FilterValidationException"></exception>
        public void Validate() {
            ValidatePathWildCard();
            InitRegex();
        }
        
        /// <summary>
        /// Returns true if the given is excluded with the current filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsFilePassingFilter(string filePath) {
            return GetExcludeRegex().All(regex => !regex.IsMatch(filePath));
        }
        
        private List<Regex> _excludeRegexes;
        
        protected List<Regex> GetExcludeRegex() {
            if (_excludeRegexes == null) {
                InitRegex();
            }
            return _excludeRegexes;
        }
        
        protected virtual void ValidatePathWildCard() {
            foreach (var pathWildCard in Exclude.Split(';')) {
                try {
                    Utils.ValidatePathWildCard(pathWildCard);
                } catch (Exception e) {
                    throw new FilterValidationException($"Invalid path filter, reason : {e.Message}, please check the following string : {pathWildCard.PrettyQuote()}");
                }
            }
        }
        
        private void InitRegex() {
            _excludeRegexes = new List<Regex>();
            foreach (var regexString in GetRegexStrings()) {
                try {
                    _excludeRegexes.Add(new Regex(regexString));
                } catch (Exception e) {
                    throw new FilterValidationException( $"Invalid filter regex expression {regexString.PrettyQuote()}, reason : {e.Message}");
                }
            }
        }
    }
}