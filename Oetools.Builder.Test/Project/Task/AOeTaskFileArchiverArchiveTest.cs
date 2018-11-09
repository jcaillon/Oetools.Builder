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
using System.Linq;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.Project.Task {
    
    [TestClass]
    public class AOeTaskFileArchiverArchiveTest {
        
        [TestMethod]
        public void SetTargets() {
            var task = new AOeTaskFileArchiverArchive2 {
                Include = "**",
                NewTargetFunc = null
            };
            
            Assert.AreEqual(0, task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").ToList().Count);

            task.ArchivePath = "archive.pack";
            task.TargetDirectory = "dir";
            
            var list = task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").Select(s => s.GetTargetPath()).ToList();
            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\archive.pack\dir\source.txt")));
            
            task.TargetFilePath = @"dir\newfilename";
            
            Assert.AreEqual(2, task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").ToList().Count);
            
            task.ArchivePath = "archive.pack;archive2.pack";
            
            Assert.AreEqual(4, task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").ToList().Count);
            
            task.Include = @"C:\((**))((*)).((*));**";
            task.ArchivePath = @"archive.pack;/arc.{{3}}";
            task.TargetFilePath = "{{1}}file.{{3}}";
            task.TargetDirectory = null;

            list = task.GetTargetsFiles(@"C:\folder\source.txt", @"D:\").Select(s => s.GetTargetPath()).ToList();
            
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"D:\archive.pack\folder\file.txt")));
            Assert.IsTrue(list.Exists(s => s.Equals(@"C:\arc.txt\folder\file.txt")), "we expect to have /arc converted into C:\\");
            
            Assert.ThrowsException<TaskExecutionException>(() => task.GetTargetsFiles(@"C:\folder\source.txt", null), "This task is not allowed to target relative path because no base target directory is not defined at this moment, the error occured for : <<targetfolder>>");
            
            task.ArchivePath = @"C:\archive.pack";
            
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
            task.ArchivePath = null;
            
            list = task.GetTargetsFiles(@"C:\folder\source.txt", null).Select(s => s.FilePath).ToList();
            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list.Exists(s => s.Equals(@"C:\folder\file.txt")));
            
            list = task.GetTargetsFiles(@"C:\cool\source", @"D:\").Select(s => s.FilePath).ToList();
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
            
            Assert.ThrowsException<TaskValidationException>(() => targetTask.Validate());
            
            targetTask.ArchivePath = "needed as well";

            Assert.ThrowsException<TargetValidationException>(() => targetTask.Validate());
            
            targetTask.TargetFilePath = null;
            targetTask.TargetDirectory = "\r wrong target, invalid path";
            
            Assert.ThrowsException<TargetValidationException>(() => targetTask.Validate());

            targetTask.TargetDirectory = "ok";
            
            targetTask.Validate();
        }

        private class AOeTaskFileArchiverArchive2 : AOeTaskFileArchiverArchive {
            
            public IArchiver Archiver { get; set; }
            
            public string TargetFilePath { get; set; }
        
            public string TargetDirectory { get; set; }
            
            public string ArchivePath { get; set; }       
        
            public override ArchiveCompressionLevel GetCompressionLevel() => ArchiveCompressionLevel.None;
        
            protected override IArchiver GetArchiver() => Archiver;
        
            protected override AOeTarget GetNewTarget() => NewTargetFunc?.Invoke() ?? new OeTargetZip();

            public Func<AOeTarget> NewTargetFunc { get; set; }

            protected override string GetArchivePath() => ArchivePath;

            protected override string GetArchivePathPropertyName() => nameof(ArchivePath);

            protected override string GetTargetFilePath() => TargetFilePath;

            protected override string GetTargetFilePathPropertyName() => nameof(TargetFilePath);

            protected override string GetTargetDirectory() => TargetDirectory;

            protected override string GetTargetDirectoryPropertyName() => nameof(TargetDirectory);

            public List<AOeTarget> GetTargetsFiles(string filePath, string baseDirectory) {
                var file = new OeFile(filePath);
                var list = new PathList<OeFile> { file };
                SetTargets(list, baseDirectory);
                return list.ElementAt(0).TargetsToBuild;
            }
        }
        
    }
}