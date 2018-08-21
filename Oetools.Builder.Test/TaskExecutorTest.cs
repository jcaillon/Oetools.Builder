#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutorTest.cs) is part of Oetools.Builder.Test.
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test {
    
    [TestClass]
    public class TaskExecutorTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(TaskExecutorTest)));
                     
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
        public void TaskExecutor_Test_basic1() {
            var taskExecutor = new TaskExecutor();
            
            // won't do much, but shouldn't fail
            taskExecutor.Execute();
        }

        private bool _cancelled;
        
        [TestMethod]
        public void TaskExecutor_Test_cancel() {
            _cancelled = false;
            var taskExecutor = new TaskExecutor();
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            taskExecutor.CancelSource = cancelSource;
            taskExecutor.Tasks = new List<IOeTask> {
                new TaskWaitForCancel()
            };
            
            Task.Factory.StartNew(() => {
                Thread.Sleep(1000);
                _cancelled = true;
                cancelSource.Cancel();
            });

            try {
                taskExecutor.Execute();
            } catch (OperationCanceledException) {
            }
            Assert.IsTrue(_cancelled);
        }


        [TestMethod]
        public void TaskExecutor_Test_cancel_on_exception() {
            var taskExecutor = new TaskExecutor();
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            taskExecutor.CancelSource = cancelSource;
            taskExecutor.Tasks = new List<IOeTask> {
                new TaskExceptionAndWaitForCancel()
            };
            Assert.IsFalse(cancelSource.IsCancellationRequested);
            OperationCanceledException e = null;
            try {
                taskExecutor.Execute();
            } catch (OperationCanceledException ex) {
                e = ex;
            }
            Assert.IsNotNull(e);
            Assert.IsTrue(cancelSource.IsCancellationRequested);
        }

        [TestMethod]
        public void TaskExecutor_Test_injection() {
            var task = new TaskInjectionTest();
            Assert.IsFalse(task.IsCancelSourceSet);
            Assert.IsFalse(task.IsLogSet);
            Assert.IsFalse(task.IsFileFilter);
            var taskExecutor = new TaskExecutor();
            taskExecutor.Tasks = new List<IOeTask> {
                task
            };
            taskExecutor.Execute();
            Assert.IsTrue(task.IsCancelSourceSet);
            Assert.IsTrue(task.IsLogSet);
            Assert.IsTrue(task.IsFileFilter);
        }
        

        [TestMethod]
        public void TaskExecutor_Test_task_files() {
            var baseDir = Path.Combine(TestFolder, "taskfiles");
            Utils.CreateDirectoryIfNeeded(baseDir);
            
            File.WriteAllText(Path.Combine(baseDir, "file1.ext"), "");
            File.WriteAllText(Path.Combine(baseDir, "file2.ext"), "");
            
            var taskExecutor = new TaskExecutor();
            var task1 = new TaskOnFile {
                Include = $"{baseDir}/**",
                Exclude = "**2.ext"
            };
            var task2 = new TaskOnArchive {
                Include = $"{baseDir}/((file1)).ext;{baseDir}/((**))",
                Archive = $"{baseDir}\\archive.zip",
                RelativeTargetFilePath = "new{{1}}"
            };
            taskExecutor.Tasks = new List<IOeTask> {
                task1,
                task2
            };
            taskExecutor.Execute();
            Assert.AreEqual(1, task1.Files.Count, "only file1.ext was included");
            Assert.AreEqual(0, task1.Files[0].TargetsFiles.Count, "no files expected since we have no targets defined");
            Assert.AreEqual(2, task2.Files.Count, "we expect 2 files here");
            var task2Targets = task2.Files.SelectMany(f => f.TargetsArchives).ToList();
            Assert.AreEqual(2, task2Targets.Count, "we expect 2 targets");
            Assert.IsTrue(task2Targets.Exists(t => t.GetTargetFilePath().Equals($"{baseDir}\\archive.zip\\newfile1")));
            Assert.IsTrue(task2Targets.Exists(t => t.GetTargetFilePath().Equals($"{baseDir}\\archive.zip\\newfile2.ext")));
        }

        private class TaskOnFile : OeTaskFileTargetFile {
            public List<IOeFileToBuildTargetFile> Files { get; set; } = new List<IOeFileToBuildTargetFile>();
            protected override void ExecuteForFilesInternal(IEnumerable<IOeFileToBuildTargetFile> files) {
                Files.AddRange(files);
            }
        }
        
        private class TaskOnArchive : OeTaskFileTargetArchive {
            public List<IOeFileToBuildTargetArchive> Files { get; set; } = new List<IOeFileToBuildTargetArchive>();
            protected override void ExecuteForFilesInternal(IEnumerable<IOeFileToBuildTargetArchive> files) {
                Files.AddRange(files);
            }
            public override string GetTargetArchive() => Archive;
            public string Archive { get; set; }
        }
        
        private class TaskInjectionTest : IOeTask, IOeTaskCompile {
            public bool IsLogSet { get; private set; }
            public bool IsCancelSourceSet { get; private set; }
            public bool IsFileFilter { get; private set; }
            public void Execute() { }
            public void SetLog(ILogger log) {
                IsLogSet = true; 
            }
            public event EventHandler<TaskExceptionEventArgs> PublishException;
            public void SetCancelSource(CancellationTokenSource cancelSource) {
                IsCancelSourceSet = true;
            }
            public void SetFileExtensionFilter(string filter) {
                IsFileFilter = true;
            }
            public List<TaskExecutionException> GetExceptionList() => null;
        }
        
        private class TaskExceptionAndWaitForCancel : IOeTask {
            private CancellationTokenSource _cancelSource;
            public void Execute() {
                PublishException?.Invoke(this, new TaskExceptionEventArgs(false, new TaskExecutionException(null, "oups!")));
                _cancelSource.Token.WaitHandle.WaitOne();
                _cancelSource.Token.ThrowIfCancellationRequested();
            }
            public void SetLog(ILogger log) {}
            public event EventHandler<TaskExceptionEventArgs> PublishException;
            public void SetCancelSource(CancellationTokenSource cancelSource) {
                _cancelSource = cancelSource;
            }
            public List<TaskExecutionException> GetExceptionList() => null;
        }
        
        private class TaskWaitForCancel : IOeTask {
            private CancellationTokenSource _cancelSource;
            public void Execute() {
                _cancelSource.Token.WaitHandle.WaitOne();
                _cancelSource.Token.ThrowIfCancellationRequested();
            }
            public void SetLog(ILogger log) {}
            public event EventHandler<TaskExceptionEventArgs> PublishException;
            public void SetCancelSource(CancellationTokenSource cancelSource) {
                _cancelSource = cancelSource;
            }
            public List<TaskExecutionException> GetExceptionList() => null;
        }

    }
}