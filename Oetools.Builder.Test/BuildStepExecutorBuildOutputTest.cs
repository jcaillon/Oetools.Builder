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
using System.IO;
using System.Linq;
using DotUtilities;
using DotUtilities.Archive;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.History;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Test {

    [TestClass]
    public class BuildStepExecutorBuildOutputTest {

        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(BuildStepExecutorBuildOutputTest)));

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
        public void BuildStepExecutorBuildOutput_Test_FilesDirectoriesBuilt() {

            var outputDir = Path.Combine(TestFolder, "output1");

            Utils.CreateDirectoryIfNeeded(Path.Combine(outputDir, "sourcedir"));
            Utils.CreateDirectoryIfNeeded(Path.Combine(outputDir, "seconddir"));

            File.WriteAllText(Path.Combine(outputDir, "sourcedir", "file1.ext"), "");
            File.WriteAllText(Path.Combine(outputDir, "sourcedir", "file2.ext"), "");
            File.WriteAllText(Path.Combine(outputDir, "sourcedir", "file3.ext"), "");

            var taskExecutor = new BuildStepExecutorBuildOutput {
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        OutputDirectoryPath = outputDir
                    }
                }
            };

            var task1 = new TaskOnFile {
                Include = @"**sourcedir**",
                Exclude = "**2.ext",
                TargetFilePath = Path.Combine(outputDir, "newfile"),
                TargetDirectory = "relative"
            };

            var task2 = new TaskOnDirectory {
                Include = "**"
            };

            taskExecutor.Tasks = new List<IOeTask> {
                task1,
                task2
            };

            taskExecutor.Execute();

            Assert.AreEqual(2, task1.Files.Count, "only file1.ext and file3 were included");

            var taskTargets = task1.Files.SelectMany(f => f.TargetsToBuild).ToList();

            Assert.AreEqual(4, taskTargets.Count, "we expect 4 targets");

            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(outputDir, "relative", "file1.ext"))));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(outputDir, "relative", "file3.ext"))));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(outputDir, "newfile"))));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(outputDir, "newfile"))));


            Assert.AreEqual(2, task2.Directories.Count, "we expect 2 directories to have been built");
        }

        private class TaskOnFile : AOeTaskFileArchiverArchive {
            public IArchiver Archiver { get; set; }
            public override string TargetArchivePath { get; set; }
            public override string TargetFilePath { get; set; }
            public override string TargetDirectory { get; set; }
            protected override IArchiver GetArchiver() => Archiver;
            protected override AOeTarget GetNewTarget() => new OeTargetFile();
            public List<IOeFileToBuild> Files { get; set; } = new List<IOeFileToBuild>();
            protected override void ExecuteInternalArchive() {
                Files.AddRange(GetFilesToBuild());
            }
        }

        private class TaskOnDirectory : AOeTaskDirectory {
            public List<IOeDirectory> Directories { get; set; } = new List<IOeDirectory>();

            protected override void ExecuteInternal() {
                Directories.AddRange(GetDirectoriesToProcess());
            }

            protected override void ExecuteTestModeInternal() {
                throw new System.NotImplementedException();
            }
        }
    }
}
