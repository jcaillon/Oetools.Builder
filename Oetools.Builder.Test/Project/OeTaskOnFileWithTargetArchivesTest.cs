#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskOnFileWithTargetArchivesTest.cs) is part of Oetools.Builder.Test.
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
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project;

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class OeTaskOnFileWithTargetArchivesTest {

        [TestMethod]
        public void OeTaskOnFileWithTargetArchives_Test_Validate() {
            var task = new OeTaskOnFilesWithTargetArchives2 {
                Include = "**"
            };

            Assert.ThrowsException<TaskValidationException>(() => task.Validate());

            task.RelativeTargetDirectory = "cool";
            
            task.Validate();
        }
        
        [TestMethod]
        public void OeTaskOnFileWithTargetArchives_Test_GetTargets() {
            var task = new OeTaskOnFilesWithTargetArchives2 {
                Include = "**"
            };
            
            Assert.AreEqual(0, task.GetFileTargets(@"C:\folder\source.txt", @"D:\").SelectMany(d => d.Value).ToList().Count);

            task.ArchivePath = "archive.pack";
            task.RelativeTargetDirectory = "dir";
            
            Assert.AreEqual(1, task.GetFileTargets(@"C:\folder\source.txt", @"D:\").SelectMany(d => d.Value).ToList().Count);
            var list = task.GetFileTargets(@"C:\folder\source.txt", @"D:\").SelectMany(d => d.Value.Select(s => Path.Combine(d.Key, s))).ToList();
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\archive.pack\dir\source.txt")));
            
            task.RelativeTargetFilePath = @"dir\newfilename";
            
            Assert.AreEqual(2, task.GetFileTargets(@"C:\folder\source.txt", @"D:\").SelectMany(d => d.Value).ToList().Count);
            
            task.ArchivePath = "archive.pack;archive2.pack";
            
            Assert.AreEqual(4, task.GetFileTargets(@"C:\folder\source.txt", @"D:\").SelectMany(d => d.Value).ToList().Count);
            
            task.Include = @"C:\((**))((*)).((*));**";
            task.ArchivePath = @"archive.pack;C:\arc.pack";
            task.RelativeTargetFilePath = "{{1}}file.{{3}}";
            task.RelativeTargetDirectory = null;

            list = task.GetFileTargets(@"C:\folder\source.txt", @"D:\").SelectMany(d => d.Value.Select(s => Path.Combine(d.Key, s))).ToList();
            Assert.AreEqual(4, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\archive.pack\folder\file.txt")));
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\archive.pack\file.")));
            Assert.IsTrue(list.Exists(s => s.Equals(@"C:\arc.pack\folder\file.txt")));
            Assert.IsTrue(list.Exists(s => s.Equals(@"C:\arc.pack\file.")));
        }

        private class OeTaskOnFilesWithTargetArchives2 : OeTaskOnFilesWithTargetArchives {
            public override string GetTargetArchive() => ArchivePath;
            public string ArchivePath { get; set; }
        }
        
    }
}