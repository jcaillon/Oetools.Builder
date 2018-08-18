#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeFilterTest.cs) is part of Oetools.Builder.Test.
// 
// Oetools.Builder.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project;

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class OeFilterTest {

        [DataTestMethod]
        [DataRow(@"**/folder*", false)]
        [DataRow(@"**((/folder*", true)]
        [DataRow(@"**))/folder*", true)]
        [DataRow(@"**|", true)]
        [DataRow("\nverg", true)]
        public void OeFilter_Validate_pathWildCard_Test(string pathWildCard, bool throws) {
            var filter = new OeFilter {
                Exclude = pathWildCard
            };
            if (throws) {
                Assert.ThrowsException<FilterValidationException>(() => filter.Validate());
            } else {
                filter.Validate();
            }
        }

        [DataTestMethod]
        [DataRow(@"[dD]", false)]
        [DataRow(@"(derp", true)]
        [DataRow(@"invalidregex)", true)]
        public void OeFilter_Validate_regex_Test(string regex, bool throws) {
            var filter = new OeFilterRegex {
                Exclude = regex
            };
            if (throws) {
                Assert.ThrowsException<FilterValidationException>(() => filter.Validate());
            } else {
                filter.Validate();
            }
        }
        
    }
}