﻿#region header
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
using Oetools.Builder.History;
using Oetools.Builder.Project.Task;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.Project.Task {
    
    [TestClass]
    public class OeTaskFileDeleteTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(OeTaskFileDeleteTest)));
                     
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
        public void Execute_Test() {
            Utils.CreateDirectoryIfNeeded(TestFolder);
            var filePath = Path.Combine(TestFolder, "file");
            var filePath2 = Path.Combine(TestFolder, "file2");
            File.WriteAllText(filePath, "");
            File.WriteAllText(filePath2, "");
            
            var task = new OeTaskFileDelete();
            task.SetFilesToBuild(new FileList<OeFile> {
                new OeFile(filePath),   
                new OeFile(filePath2)   
            });
            
            task.Execute();
            
            Assert.IsFalse(File.Exists(filePath));
            Assert.IsFalse(File.Exists(filePath2));
            
            Assert.AreEqual(0, task.GetFilesBuilt()?.Count ?? 0);
        }


    }
}