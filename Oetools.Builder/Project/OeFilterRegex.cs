using System;
using System.Collections.Generic;

namespace Oetools.Builder.Project {

    [Serializable]
    public class OeFilterRegex : OeFilter {
        
        public override IEnumerable<string> GetRegexStrings() => Exclude.Split(';');

        protected override void ValidatePathWildCard() {
            // no wild cards for the regex version
        }
    }
}