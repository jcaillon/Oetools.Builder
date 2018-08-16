using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
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

        /// <summary>
        /// Get a list of exclusion regex strings from a list of filters, appending extra exclusions if needed
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="baseDirectory"></param>
        /// <param name="extraSourceDirectoryExclusions"></param>
        /// <returns></returns>
        public static List<string> GetExclusionRegexStringsFromFilters(List<OeFilter> filters, string baseDirectory = null, string extraSourceDirectoryExclusions = OeBuilderConstants.ExtraSourceDirectoryExclusions) {
            List<string> exclusionRegexStrings = null;
            if (filters != null) {
                exclusionRegexStrings = filters.SelectMany(f => f.Exclude.Split(';').Select(p => f is OeFilterRegex ? p : p.PathWildCardToRegex())).ToList();
            }

            if (!string.IsNullOrEmpty(extraSourceDirectoryExclusions) && !string.IsNullOrEmpty(baseDirectory)) {
                if (exclusionRegexStrings == null) {
                    exclusionRegexStrings = new List<string>();
                }
                exclusionRegexStrings.AddRange(extraSourceDirectoryExclusions.Split(';').Select(s => Path.Combine(baseDirectory, s).PathWildCardToRegex()));
            }
            return exclusionRegexStrings;
        }
    }
}