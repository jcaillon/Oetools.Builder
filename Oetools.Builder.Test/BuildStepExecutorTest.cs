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
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Test {
    
    [TestClass]
    public class BuildStepExecutorTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(BuildStepExecutorTest)));
                     
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
            var taskExecutor = new BuildStepExecutor();
            
            // won't do much, but shouldn't fail
            taskExecutor.Execute();
        }

        [TestMethod]
        public void TaskExecutor_Test_cancel() {
            var taskExecutor = new BuildStepExecutor();
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            taskExecutor.CancelSource = cancelSource;
            taskExecutor.Tasks = new List<IOeTask> {
                new TaskWaitForCancel()
            };
            
            Task.Factory.StartNew(() => {
                Thread.Sleep(1000);
                cancelSource.Cancel();
            });

            Exception ex = null;
            try {
                taskExecutor.Execute();
            } catch (OperationCanceledException e) {
                ex = e;
            }
            Assert.IsNotNull(ex);
        }

        [TestMethod]
        public void TaskExecutor_Test_task_warning() {
            var taskExecutor = new BuildStepExecutor {
                Tasks = new List<IOeTask> {
                    new TaskWarning()
                }
            };
            TaskExecutorException e = null;
            try {
                taskExecutor.Execute();
            } catch (TaskExecutorException ex) {
                e = ex;
            }
            Assert.IsNull(e);
            Assert.IsNotNull(taskExecutor.Tasks.ToList()[0].GetExceptionList());
            Assert.AreEqual(1, taskExecutor.Tasks.ToList()[0].GetExceptionList().Count);

            // TreatWarningsAsErrors
            
            taskExecutor.Properties = new OeProperties {
                BuildOptions = new OeBuildOptions {
                    TreatWarningsAsErrors = true
                }
            };
            try {
                taskExecutor.Execute();
            } catch (TaskExecutorException ex) {
                e = ex;
            }
            Assert.IsNotNull(e);
        }

        [TestMethod]
        public void TaskExecutor_Test_task_exception() {
            var taskExecutor = new BuildStepExecutor {
                Tasks = new List<IOeTask> {
                    new TaskException()
                }
            };
            TaskExecutorException e = null;
            try {
                taskExecutor.Execute();
            } catch (TaskExecutorException ex) {
                e = ex;
            }
            Assert.IsNotNull(e);
        }

        [TestMethod]
        public void TaskExecutor_Test_injection() {
            var task = new TaskInjectionTest();
            Assert.IsFalse(task.IsCancelSourceSet);
            Assert.IsFalse(task.IsLogSet);
            Assert.IsFalse(task.IsTestSet);
            Assert.IsFalse(task.IsFilesCompiledSet);
            Assert.IsFalse(task.IsPropertySet);
            var taskExecutor = new BuildStepExecutor {
                Tasks = new List<IOeTask> {
                    task
                },
                CancelSource = new CancellationTokenSource(),
                Log = new Log2(),
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        TestMode = true
                    }
                }
            };
            taskExecutor.Execute();
            Assert.IsTrue(task.IsCancelSourceSet);
            Assert.IsTrue(task.IsLogSet);
            Assert.IsTrue(task.IsTestSet);
            Assert.IsTrue(task.IsPropertySet);
            Assert.IsFalse(task.IsFilesCompiledSet);
        }

        private class Log2 : ILogger {
            public void Fatal(string message, Exception e = null) {}
            public void Error(string message, Exception e = null) {}
            public void Warn(string message, Exception e = null) {}
            public void Success(string message, Exception e = null) {}
            public void Info(string message, Exception e = null) {}
            public void Debug(string message, Exception e = null) {}
            public ITraceLogger Trace => null;
            public void ReportProgress(int max, int current, string message) {}
            public void ReportGlobalProgress(int max, int current, string message) {
                throw new NotImplementedException();
            }
        }
        

        [TestMethod]
        public void TaskExecutor_Test_task_files() {
            var baseDir = Path.Combine(TestFolder, "taskfiles");
            Utils.CreateDirectoryIfNeeded(baseDir);
            
            File.WriteAllText(Path.Combine(baseDir, "file1.ext"), "");
            File.WriteAllText(Path.Combine(baseDir, "file2.ext"), "");
            
            var taskExecutor = new BuildStepExecutor();
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
            Assert.IsTrue(task2Targets.Exists(t => t.GetTargetPath().Equals($"{baseDir}\\archive.zip\\newfile1")));
            Assert.IsTrue(task2Targets.Exists(t => t.GetTargetPath().Equals($"{baseDir}\\archive.zip\\newfile2.ext")));
        }

        private class TaskOnFile : OeTaskFileTargetFile {
            public List<IOeFileToBuildTargetFile> Files { get; set; } = new List<IOeFileToBuildTargetFile>();
            public override void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files) {
                Files.AddRange(files);
            }
        }
        
        private class TaskOnArchive : OeTaskFileTargetArchive {
            public List<IOeFileToBuildTargetArchive> Files { get; set; } = new List<IOeFileToBuildTargetArchive>();
            public override void ExecuteForFilesTargetArchives(IEnumerable<IOeFileToBuildTargetArchive> files) {
                Files.AddRange(files);
            }
            public override string GetTargetArchive() => Archive;
            public string Archive { get; set; }
            protected override OeTargetArchive GetNewTargetArchive() => new OeTargetArchiveZip();
        }
        
        private class TaskInjectionTest : OeTaskFile, IOeTaskCompile {
            public bool IsLogSet => Log != null;
            public bool IsCancelSourceSet => CancelSource != null;
            public bool IsTestSet => TestMode;
            public bool IsFilesCompiledSet { get; private set; }
            public bool IsPropertySet => GetProperties() != null;
            public void SetCompiledFiles(FileList<UoeCompiledFile> compiledFile) {
                IsFilesCompiledSet = true;
            }
            public FileList<UoeCompiledFile> GetCompiledFiles() => null;
        }
        
        private class TaskException : OeTask {
            public override void Validate() { }
            protected override void ExecuteInternal() {
                AddExecutionErrorAndThrow(new TaskExecutionException(this, "oups!"));
            }
        }
        
        private class TaskWarning : OeTask {
            public override void Validate() { }
            protected override void ExecuteInternal() {
                AddExecutionWarning(new TaskExecutionException(this, "oups warning!"));
            }
        }
        
        private class TaskWaitForCancel : IOeTask {
            private CancellationTokenSource _cancelSource;
            public void Validate() {}

            public void Execute() {
                _cancelSource.Token.WaitHandle.WaitOne();
                _cancelSource.Token.ThrowIfCancellationRequested();
            }
            public void SetLog(ILogger log) {}
            public void SetTestMode(bool testMode) { }
            public event EventHandler<TaskWarningEventArgs> PublishWarning;
            public void SetCancelSource(CancellationTokenSource cancelSource) {
                _cancelSource = cancelSource;
                PublishWarning?.Invoke(null, null);
            }
            public List<TaskExecutionException> GetExceptionList() => null;
        }

    }
}