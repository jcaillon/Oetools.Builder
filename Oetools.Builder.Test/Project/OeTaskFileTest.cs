#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTest.cs) is part of Oetools.Builder.Test.
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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Project.Task;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class OeTaskFileTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(OeTaskFileTest)));
                     
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
            var task = new OeTaskOnFile2 {
                IncludeRegex = ".*"
            };
            
            Assert.AreEqual(0, task.GetIncludedFiles().Count);

            Utils.CreateDirectoryIfNeeded(Path.Combine(TestFolder, "folder", "sub"));
            File.WriteAllText(Path.Combine(TestFolder, "folder", "sub", "file"), "");

            task.Include = "**";
            
            Assert.AreEqual(0, task.GetIncludedFiles().Count, "we can't match any existing file or folder with this");

            task.Include = Path.Combine(TestFolder, "folder", "**");
            
            Assert.AreEqual(1, task.GetIncludedFiles().Count, "we match the only file there is");
            Assert.IsTrue(task.GetIncludedFiles().ToList().Exists(s => s.FilePath.ToCleanPath().Equals(Path.Combine(TestFolder, "folder", "sub", "file").ToCleanPath())));

            task.Include = $"{task.Include};{Path.Combine(TestFolder, "folder", "sub", "file")}";
            
            Assert.AreEqual(1, task.GetIncludedFiles().Count, "we added a direct file path. However, this method only returns unique files");

            task.Include = Path.Combine(TestFolder, "folder", "sub", "file");
            
            Assert.AreEqual(1, task.GetIncludedFiles().Count, "should be good alone");
        }

        private class OeTaskOnFile2 : OeTaskFile { }

    }
}