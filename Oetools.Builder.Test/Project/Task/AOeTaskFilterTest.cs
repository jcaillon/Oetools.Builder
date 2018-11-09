#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFilterTest.cs) is part of Oetools.Builder.Test.
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
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Test.Project.Task {
    
    [TestClass]
    public class AOeTaskFilterTest {
        
        
        [TestMethod]
        public void OeTaskFilter_Test_IsFileIncluded() {
            var filter = new OeFilterOptions();

            Assert.AreEqual(true, filter.IsPathIncluded("myderpfile"));

            filter.Include = "**";
            
            Assert.AreEqual(true, filter.IsPathIncluded("ending.txt"));
            
            filter.Include = "**.derp";
            
            Assert.AreEqual(false, filter.IsPathIncluded(@"C:\folder\fold\derp\file.cool"));
            Assert.AreEqual(true, filter.IsPathIncluded(@"C:\folder\fold\derp\file.derp"));
            
            filter.Include = "**.nice";
            filter.IncludeRegex = ".*cool.*";
            
            Assert.AreEqual(false, filter.IsPathIncluded(@"C:\folder\fold\derp\.nice.file"));
            Assert.AreEqual(true, filter.IsPathIncluded(@"C:\folder\fold\derp\.nice"));
            Assert.AreEqual(true, filter.IsPathIncluded(@"C:\folder\fold\cool\.nice.file"));

        }
        
        [TestMethod]
        public void OeTaskFilter_Test_IsFileExcluded() {
            var filter = new OeFilterOptions {
                Exclude = "**.txt;**derp**"
            };

            Assert.AreEqual(true, filter.IsPathExcluded("myderpfile"));
            Assert.AreEqual(true, filter.IsPathExcluded("ending.txt"));
            Assert.AreEqual(true, filter.IsPathExcluded(@"C:\folder\fold\derp\file.cool"));
            Assert.AreEqual(false, filter.IsPathExcluded(@"C:\folder\fold\file.cool"));
        }
        
        [TestMethod]
        public void OeTaskFilter_Test_IsFilePassingFilter() {
            var filter = new OeFilterOptions {
                Include = "**/subfolder/**",
                IncludeRegex = ".*cool.*",
                Exclude = "**derp**",
                ExcludeRegex = "^.*\\.txt$"
            };

            Assert.AreEqual(true, filter.IsPathPassingFilter(@"C:\folder\fold\file.cool"));
            Assert.AreEqual(false, filter.IsPathPassingFilter(@"C:\folder\derp\file.cool"));
            Assert.AreEqual(false, filter.IsPathPassingFilter(@"C:\folder\fold\file.random"));
            Assert.AreEqual(true, filter.IsPathPassingFilter(@"C:\folder\subfolder\file.random"));
            Assert.AreEqual(false, filter.IsPathPassingFilter(@"C:\folder\subfolder\file.txt"));
        }

        [TestMethod]
        public void OeTaskFilter_Test_GetRegexIncludeStrings() {
            var filter = new OeFilterOptions();
            Assert.AreEqual(0, filter.GetRegexIncludeStrings().Count);
            Assert.AreEqual(0, filter.GetRegexExcludeStrings().Count);
            Assert.AreEqual(0, filter.GetIncludeStrings().Count);
            Assert.AreEqual(0, filter.GetExcludeStrings().Count);

            filter.Exclude = "**/file.txt;**derp.txt";
            
            Assert.AreEqual(0, filter.GetRegexIncludeStrings().Count);
            Assert.AreEqual(2, filter.GetRegexExcludeStrings().Count);
            Assert.AreEqual(0, filter.GetIncludeStrings().Count);
            Assert.AreEqual(2, filter.GetExcludeStrings().Count);
            
            filter.Include = "**/subfolder/**";
            filter.IncludeRegex = ".*cool.*";
            
            Assert.AreEqual(2, filter.GetRegexIncludeStrings().Count);
            Assert.AreEqual(2, filter.GetRegexExcludeStrings().Count);
            Assert.AreEqual(1, filter.GetIncludeStrings().Count);
            Assert.AreEqual(2, filter.GetExcludeStrings().Count);
        }

        [DataTestMethod]
        [DataRow(@"**/folder*", false)]
        [DataRow(@"**((/folder*", true)]
        [DataRow(@"**))/folder*", true)]
        [DataRow(@"**||", false)]
        [DataRow("\nverg", true)]
        public void OeTaskFilter_Validate_pathWildCard_Test(string pathWildCard, bool throws) {
            var filter = new OeFilterOptions {
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
        public void OeTaskFilter_Validate_regex_Test(string regex, bool throws) {
            var filter = new OeFilterOptions {
                ExcludeRegex = regex
            };
            if (throws) {
                Assert.ThrowsException<FilterValidationException>(() => filter.Validate());
            } else {
                filter.Validate();
            }
        }
        
    }
}