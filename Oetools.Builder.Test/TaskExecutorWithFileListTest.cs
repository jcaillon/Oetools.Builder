#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutorWithFileListTest.cs) is part of Oetools.Builder.Test.
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.History;
using Oetools.Builder.Project;

namespace Oetools.Builder.Test {
    
    [TestClass]
    public class TaskExecutorWithFileListTest {
        
        [TestMethod]
        public void TaskExecutorWithFileListTest_Test_task_files() {
            var taskExecutor = new TaskExecutorWithFileList {
                TaskFiles = new List<OeFile> {
                    new OeFile { SourceFilePath = @"C:\sourcedir\file1.ext" },
                    new OeFile { SourceFilePath = @"C:\sourcedir\file2.ext" },
                    new OeFile { SourceFilePath = @"C:\sourcedir\file3.ext" }
                },
                OutputDirectory = "outputdir"
            };
            var task1 = new TaskOnFile {
                Include = @"C:\sourcedir**",
                Exclude = "**2.ext",
                TargetFilePath = @"C:\newfile",
                TargetDirectory = "relative"
            };
            taskExecutor.Tasks = new List<IOeTask> {
                task1
            };
            taskExecutor.Execute();
            Assert.AreEqual(2, task1.Files.Count, "only file1.ext and file3 were included");
            var taskTargets = task1.Files.SelectMany(f => f.TargetsFiles).ToList();
            Assert.AreEqual(4, taskTargets.Count, "we expect 4 targets");
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetFilePath().Equals(@"outputdir\relative\file1.ext")));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetFilePath().Equals(@"outputdir\relative\file3.ext")));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetFilePath().Equals(@"C:\newfile")));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetFilePath().Equals(@"C:\newfile")));
        }
        
        private class TaskOnFile : OeTaskFileTargetFile {
            public List<IOeFileToBuildTargetFile> Files { get; set; } = new List<IOeFileToBuildTargetFile>();
            protected override void ExecuteForFilesInternal(IEnumerable<IOeFileToBuildTargetFile> files) {
                Files.AddRange(files);
            }
        }
    }
}