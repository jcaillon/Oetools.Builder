#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskOnFileWithTargetTest.cs) is part of Oetools.Builder.Test.
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

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class OeTaskOnFileWithTargetTest {

        [TestMethod]
        public void OeTaskFileTargetFileTest() {
            var targetTask = new OeTaskOnFilesWithTarget2();

            Assert.ThrowsException<TaskValidationException>(() => targetTask.Validate());

            targetTask.Include = "**";
            
            Assert.ThrowsException<TaskValidationException>(() => targetTask.Validate());

            targetTask.TargetFilePath = "{{wrong var usage";
            
            Assert.ThrowsException<TargetValidationException>(() => targetTask.Validate());

            targetTask.TargetFilePath = null;
            targetTask.TargetDirectory = "\r wrong target, invalid path";
            
            Assert.ThrowsException<TargetValidationException>(() => targetTask.Validate());

            targetTask.TargetDirectory = "ok";
            
            targetTask.Validate();
        }

        [TestMethod]
        public void OeTaskOnFileWithTarget_Test_GetFileTargets() {
            var targetTask = new OeTaskOnFilesWithTarget2 {
                Include = "**"
            };

            Assert.AreEqual(0, targetTask.GetFileTargets(@"C:\folder\source.txt", null).Count);

            targetTask.TargetDirectory = @"targetfolder";

            Assert.ThrowsException<TaskExecutionException>(() => targetTask.GetFileTargets(@"C:\folder\source.txt", null), "This task is not allowed to target relative path because no base target directory is defined at this moment, the error occured for : <<targetfolder>>");
            
            targetTask.TargetFilePath = @"targetfolder\newfilename.txt";
            
            Assert.AreEqual(2, targetTask.GetFileTargets(@"C:\folder\source.txt", @"D:\").Count);
            
            targetTask.TargetFilePath = @"targetfolder\newfilename.txt;secondtarget\filename.src";
            
            Assert.AreEqual(3, targetTask.GetFileTargets(@"C:\folder\source.txt", @"D:\").Count);

            targetTask.Include = "((**))((*)).((*));**";
            targetTask.TargetDirectory = null;
            targetTask.TargetFilePath = "{{1}}file.{{3}}";

            Assert.AreEqual(1, targetTask.GetFileTargets(@"C:\folder\source.txt", @"D:\").Count);
            Assert.IsTrue(targetTask.GetFileTargets(@"C:\folder\source.txt", @"D:\").Exists(s => s.GetTargetPath().Equals(@"C:\folder\file.txt")));

        }

        private class OeTaskOnFilesWithTarget2 : OeTaskFileTargetFile {
            
        }
    }
}