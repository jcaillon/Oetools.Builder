#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTargetArchiveTest.cs) is part of Oetools.Builder.Test.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.Project.Task {
    
    [TestClass]
    public class AOeTaskFileArchiverArchiveTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(AOeTaskFileArchiverArchiveTest)));
                     
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
        public void SetFilesToProcess() {
            var task = new AOeTaskFileArchiverArchive2 {
                Include = "**", 
                TargetArchivePath = "archive.pack", 
                TargetDirectory = "dir"
            };
            task.SetTargetBaseDirectory(@"D:\");
            task.SetFilesToProcess(new PathList<IOeFile> { new OeFile(@"C:\folder\source.txt") });

            var targets = task.GetFilesToBuild().SelectMany(s => s.TargetsToBuild).Select(t => t.GetTargetPath()).ToList();
            Assert.AreEqual(1, targets.Count);
            Assert.IsTrue(targets.Exists(s => s.Equals(@"D:\archive.pack\dir\source.txt")));
        }
        
        

        [TestMethod]
        public void SetTargets_noInclude() {
            var task = new AOeTaskFileArchiverArchive2 {
                Exclude = "**fuck**",
                NewTargetFunc = null,
                TargetArchivePath = "archive.pack",
                TargetDirectory = "dir"
            };
            
            var list = task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").Select(s => s.GetTargetPath()).ToList();
            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\archive.pack\dir\source.txt")));
        }

        [TestMethod]
        public void SetTargets() {
            var task = new AOeTaskFileArchiverArchive2 {
                Include = "**",
                NewTargetFunc = null
            };
            
            Assert.AreEqual(0, task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").ToList().Count);

            task.TargetArchivePath = "archive.pack";
            task.TargetDirectory = "dir";
            
            var list = task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").Select(s => s.GetTargetPath()).ToList();
            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\archive.pack\dir\source.txt")));
            
            task.TargetFilePath = @"dir\newfilename";
            
            Assert.AreEqual(2, task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").ToList().Count);
            
            task.TargetArchivePath = "archive.pack;archive2.pack";
            
            Assert.AreEqual(4, task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").ToList().Count);
            
            task.Include = @"C:\((**))((*)).((*));**";
            task.TargetArchivePath = @"archive.pack;/arc.{{3}}";
            task.TargetFilePath = "{{1}}file.{{3}}";
            task.TargetDirectory = null;

            list = task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").Select(s => s.GetTargetPath()).ToList();
            
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\archive.pack\folder\file.txt")));
            Assert.IsTrue(list.Exists(s => s.Equals(@"C:\arc.txt\folder\file.txt")), "we expect to have /arc converted into C:\\");
            
            Assert.ThrowsException<TaskExecutionException>(() => task.GetTargetsFiles(@"C:\folder\source.txt", null), "This task is not allowed to target relative path because no base target directory is not defined at this moment, the error occured for : <<targetfolder>>");
            
            task.TargetArchivePath = @"C:\archive.pack";
            
            Assert.AreEqual(1, task.GetTargetsFiles(@"C:\folder\source.txt", null).Count);
            
            task.TargetDirectory = @"targetfolder";
            task.TargetFilePath = @"targetfolder\newfilename.txt";
            
            Assert.AreEqual(2, task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").Count);
            
            task.TargetFilePath = @"targetfolder\newfilename.txt;secondtarget\filename.src";
            
            Assert.AreEqual(3, task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").Count);

            task.NewTargetFunc = () => new OeTargetFile();

            task.Include = "((**))((*)).((*));**";
            task.TargetDirectory = null;
            task.TargetFilePath = "{{1}}file.{{3}}";
            task.TargetArchivePath = null;
            
            list = task.GetTargetsFiles(@"C:\folder\source.txt", null).Select(s => s.FilePathInArchive).ToList();
            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"C:\folder\file.txt")));
            
            list = task.GetTargetsFiles(@"C:\cool\source", @"D:\").Select(s => s.FilePathInArchive).ToList();
            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\file")), "The final . disappears on windows.");
        }
        
        [TestMethod]
        public void Validate() {
            var targetTask = new AOeTaskFileArchiverArchive2();

            Assert.ThrowsException<TaskValidationException>(() => targetTask.Validate());

            targetTask.Include = "**";
            
            Assert.ThrowsException<TaskValidationException>(() => targetTask.Validate());

            targetTask.TargetFilePath = "{{wrong var usage";
            
            Assert.ThrowsException<TargetValidationException>(() => targetTask.Validate());
            
            targetTask.TargetArchivePath = "needed as well";

            Assert.ThrowsException<TargetValidationException>(() => targetTask.Validate());
            
            targetTask.TargetFilePath = null;
            targetTask.TargetDirectory = "\r wrong target, invalid path";
            
            Assert.ThrowsException<TargetValidationException>(() => targetTask.Validate());

            targetTask.TargetDirectory = "ok";
            
            targetTask.Validate();
        }

        private class AOeTaskFileArchiverArchive2 : AOeTaskFileArchiverArchive {
            public override string TargetArchivePath { get; set; }
            public override string TargetFilePath { get; set; }
            public override string TargetDirectory { get; set; }
            protected override IArchiver GetArchiver() => null;
            protected override AOeTarget GetNewTarget() => NewTargetFunc?.Invoke() ?? new OeTargetZip();
            public Func<AOeTarget> NewTargetFunc { get; set; }
            public List<AOeTarget> GetTargetsFiles(string filePath, string baseDirectory) {
                var file = new OeFile(filePath);
                var list = new PathList<IOeFileToBuild> { file };
                SetTargets(list, baseDirectory);
                return list.ElementAt(0).TargetsToBuild;
            }
        }
        
        [TestMethod]
        public void FilesBuilts() {
            var task = new TestTaskFileArchiverArchive {
                Include = "**",
                TargetDirectory = "",
                TargetArchivePath = "archive.test"
            };
            task.SetTargetBaseDirectory("/target");
            task.SetFilesToProcess(new PathList<IOeFile> { new OeFile(@"/file1"), new OeFile(@"/file2") });
            task.Validate();
            task.Execute();
            var builtFiles = task.GetBuiltFiles();
            
            Assert.AreEqual(1, builtFiles.Count, "Expect 1 file built.");
            Assert.IsTrue(builtFiles.ElementAt(0).Targets.ElementAt(0).GetTargetPath().PathEquals("/target/archive.test/file1".ToCleanPath()));
        }
        
        [TestMethod]
        public void ExecuteFromIncludedFiles() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var sourceDirectory = TestFolder;
            Utils.CreateDirectoryIfNeeded(Path.Combine(sourceDirectory, "subfolder"));
            
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "quit."); // compile ok
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "quit. quit."); // compile with warnings
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file3.p"), "quit.");
            
            var task = new TestTaskFileArchiverArchiveCompile {
                Include = Path.Combine(TestFolder, "((**))"),
                TargetDirectory = "",
                TargetArchivePath = "archive.test"
            };
            
            task.SetProperties(new OeProperties { BuildOptions = new OeBuildOptions { SourceDirectoryPath = sourceDirectory }});
            task.SetTargetBaseDirectory("/target");
            task.SetFilesToProcess(task.GetFilesToProcessFromIncludes());
            task.Validate();
            task.Execute();
            var builtFiles = task.GetBuiltFiles();
            
            Assert.AreEqual(1, builtFiles.Count, "Expect 1 file built.");
            Assert.IsTrue(builtFiles.ElementAt(0).Targets.ElementAt(0).GetTargetPath().PathEquals("/target/archive.test/file1.r".ToCleanPath()));
        }
        
        private class TestTaskFileArchiverArchive : AOeTaskFileArchiverArchive {
            public override string TargetArchivePath { get; set; }
            public override string TargetFilePath { get; set; }
            public override string TargetDirectory { get; set; }
            protected override IArchiver GetArchiver() => new TestArchiver();
            protected override AOeTarget GetNewTarget() => new TestTarget();
        }
        
        private class TestTaskFileArchiverArchiveCompile : TestTaskFileArchiverArchive, IOeTaskCompile {
        }

        private class TestArchiver : IArchiver {
            public void SetCancellationToken(CancellationToken? cancelToken) {}
            public event EventHandler<ArchiverEventArgs> OnProgress;
            public int ArchiveFileSet(IEnumerable<IFileToArchive> filesToPack) {
                filesToPack.First().Processed = true;
                OnProgress?.Invoke(this, new ArchiverEventArgs());
                return 1;
            }
            public int ExtractFileSet(IEnumerable<IFileInArchiveToExtract> filesToExtract) {
                filesToExtract.First().Processed = true;
                return 1;
            }
            public int DeleteFileSet(IEnumerable<IFileInArchiveToDelete> filesToDelete) {
                filesToDelete.First().Processed = true;
                return 1;
            }
            public IEnumerable<IFileInArchive> ListFiles(string archivePath) {
                return null;
            }
            public int MoveFileSet(IEnumerable<IFileInArchiveToMove> filesToMove) {
                filesToMove.First().Processed = true;
                return 1;
            }
        }

        private class TestTarget : AOeTarget {
            public override string ArchiveFilePath { get; set; }
            public override string FilePathInArchive { get; set; }
        }
    }
}