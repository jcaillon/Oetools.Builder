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

using System;
using DotUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Test.Project.Task {

    [TestClass]
    public class OeTaskExecTest {

        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(OeTaskExecTest)));

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
            if (!Utils.IsRuntimeWindowsPlatform) {
                return;
            }

            var task = new OeTaskExec {
                ExecutableFilePath = @"C:\Windows\System32\net.exe",
                Parameters = "use"
            };
            task.Execute();

            task.Parameters = "use 7874987498";

            Exception ex = null;
            try {
                task.Execute();
            } catch (Exception e) {
                ex = e;
            }
            Assert.IsNotNull(ex);

            task.IgnoreExitCode = true;

            task.Execute();
        }


    }
}
