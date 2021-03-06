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
using DotUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Test.Project.Task {

    [TestClass]
    public class AOeTaskDirectoryTest {

        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(AOeTaskDirectoryTest)));

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
        public void ValidateCanGetDirectoriesToBuildFromIncludes() {
            var task = new OeTaskOnDirectory2();

            task.Validate();
            Assert.ThrowsException<TaskValidationException>(() => task.ValidateCanGetDirectoriesToProcessFromIncludes());

            task.IncludeRegex = ".*";

            Assert.ThrowsException<TaskValidationException>(() => task.ValidateCanGetDirectoriesToProcessFromIncludes());
        }

        [TestMethod]
        public void GetDirectoriesToBuildFromIncludes() {
            var task = new OeTaskOnDirectory2();

            Assert.AreEqual(0, task.GetDirectoriesToProcessFromIncludes().Count);

            Utils.CreateDirectoryIfNeeded(Path.Combine(TestFolder, "boom", "sub"));
            Utils.CreateDirectoryIfNeeded(Path.Combine(TestFolder, "folder", "sub1"));
            Utils.CreateDirectoryIfNeeded(Path.Combine(TestFolder, "folder2", "sub2"));
            Utils.CreateDirectoryIfNeeded(Path.Combine(TestFolder, "folder3", "sub3"));

            task.Include = "**";

            Assert.AreEqual(0, task.GetDirectoriesToProcessFromIncludes().Count, "we can't match any existing file or folder with this");

            Assert.AreEqual(1, task.GetRuntimeExceptionList().Count, "the task should have published 1 warning");

            task.Include = Path.Combine(TestFolder, "folder**");
            task.IncludeRegex = null;

            task.ValidateCanGetDirectoriesToProcessFromIncludes();

            Assert.AreEqual(6, task.GetDirectoriesToProcessFromIncludes().Count, "we should match the 6 dir");

            task.Exclude = "**2";

            Assert.AreEqual(4, task.GetDirectoriesToProcessFromIncludes().Count, "now 4 subfolders");

            task.Exclude = "**2;**1";

            Assert.AreEqual(3, task.GetDirectoriesToProcessFromIncludes().Count, "now 3 subfolder");

            Assert.IsTrue(task.GetDirectoriesToProcessFromIncludes().ToList().Exists(s => s.Path.ToCleanPath().Equals(Path.Combine(TestFolder, "folder3", "sub3").ToCleanPath())));

            task.Include = $"{task.Include};{Path.Combine(TestFolder, "boom", "sub")}";

            Assert.AreEqual(4, task.GetDirectoriesToProcessFromIncludes().Count, "we added a direct file path");

            task.Include = $"{task.Include};{Path.Combine(TestFolder, "boom", "sub")}";

            Assert.AreEqual(4, task.GetDirectoriesToProcessFromIncludes().Count, "we added another direct file path. However, this method only returns unique files");

            task.Include = Path.Combine(TestFolder, "folder3", "sub3");
            task.Exclude = null;

            Assert.AreEqual(1, task.GetDirectoriesToProcessFromIncludes().Count, "should be good alone");
        }

        private class OeTaskOnDirectory2 : AOeTaskDirectory {
            protected override void ExecuteInternal() {
                throw new System.NotImplementedException();
            }

            protected override void ExecuteTestModeInternal() {
                throw new System.NotImplementedException();
            }
        }

    }
}
