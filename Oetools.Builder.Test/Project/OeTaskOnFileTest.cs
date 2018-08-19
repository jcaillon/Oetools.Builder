#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskOnFileTest.cs) is part of Oetools.Builder.Test.
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

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class OeTaskOnFileTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(OeTaskOnFileTest)));
                     
        [ClassInitialize]
        public static void Init(TestContext context) {
            Cleanup();
            Utils.CreateDirectoryIfNeeded(TestFolder);
        }


        [ClassCleanup]
        public static void Cleanup() {
            Utils.DeleteDirectoryIfExists(TestFolder, true);
        }

        [TestMethod]
        public void OeTaskOnFile_Test_() {
            var task = new OeTaskOnFile2() {
                IncludeRegex = ".*"
            };
            
            Assert.AreEqual(0, task.GetIncludedPathToList().Count);

            Utils.CreateDirectoryIfNeeded(Path.Combine(TestFolder, "folder", "sub"));
            File.WriteAllText(Path.Combine(TestFolder, "folder", "sub", "file"), "");

            task.Include = "**";
            
            Assert.AreEqual(0, task.GetIncludedPathToList().Count, "we can't match any existing file or folder with this");

            task.Include = Path.Combine(TestFolder, "folder", "**");
            
            Assert.AreEqual(1, task.GetIncludedPathToList().Count, "we match a folder");
            Assert.IsTrue(task.GetIncludedPathToList().Exists(s => s.ToCleanPath().Equals(Path.Combine(TestFolder, "folder").ToCleanPath())));

            task.Include = $"{task.Include};{Path.Combine(TestFolder, "folder", "sub", "file")}";
            
            Assert.AreEqual(2, task.GetIncludedPathToList().Count, "we added an existing file");
        }

        private class OeTaskOnFile2 : OeTaskOnFiles { }

    }
}