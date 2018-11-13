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
using Oetools.Builder.Test.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Archive;
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
            taskExecutor.CancelToken = cancelSource.Token;
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
            Assert.IsNotNull(taskExecutor.Tasks.ToList()[0].GetRuntimeExceptionList());
            Assert.AreEqual(1, taskExecutor.Tasks.ToList()[0].GetRuntimeExceptionList().Count);

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
        public void TaskExecutor_Test_SetPathsAndTargets() {
            var task = new TaskSetPathAndTargetTest();
            Assert.AreEqual(0, task.Total);
            var taskExecutor = new BuildStepExecutor {
                Tasks = new List<IOeTask> {
                    task
                }
            };
            taskExecutor.Execute();
            Assert.AreEqual(12, task.Total, "One of the following methods were not called : set files, get files to include.");
            
            var task2 = new TaskSetPathAndTargetTest2();
            Assert.AreEqual(0, task2.Total);
            taskExecutor = new BuildStepExecutor {
                Tasks = new List<IOeTask> {
                    task2
                }
            };
            taskExecutor.Execute();
            Assert.AreEqual(7, task2.Total, "One of the following methods were not called : set directories, get directories to include or dispose task.");
        }

        [TestMethod]
        public void TaskExecutor_Test_injection() {
            var task = new TaskInjectionTest();
            Assert.IsFalse(task.IsCancelSourceSet);
            Assert.IsFalse(task.IsLogSet);
            Assert.IsFalse(task.IsTestSet);
            Assert.IsFalse(task.IsFilesCompiledSet);
            Assert.IsFalse(task.IsBaseDirectorySet);
            Assert.IsFalse(task.IsPropertySet);
            var taskExecutor = new BuildStepExecutor {
                Tasks = new List<IOeTask> {
                    task
                },
                CancelToken = new CancellationTokenSource().Token,
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
            Assert.IsFalse(task.IsFilesCompiledSet, "test mode is on.");
            Assert.IsTrue(task.IsBaseDirectorySet);
        }

        private class Log2 : ILogger {
            public void Fatal(string message, Exception e = null) {}
            public void Error(string message, Exception e = null) {}
            public void Warn(string message, Exception e = null) {}
            public void Done(string message, Exception e = null) {}
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
            var task1 = new TaskOnArchive {
                Include = $"{baseDir}/**",
                Exclude = "**2.ext"
            };
            var task2 = new TaskOnArchive {
                Include = $"{baseDir}/((file1)).ext;{baseDir}/((**))",
                TargetArchivePath = $"{baseDir}\\archive.zip",
                TargetFilePath = "new{{1}}"
            };
            taskExecutor.Tasks = new List<IOeTask> {
                task1,
                task2
            };
            taskExecutor.Execute();
            Assert.AreEqual(1, task1.Files.Count, "only file1.ext was included");
            Assert.AreEqual(0, task1.Files[0].TargetsToBuild.Count, "no files expected since we have no targets defined");
            Assert.AreEqual(2, task2.Files.Count, "we expect 2 files here");
            var task2Targets = task2.Files.SelectMany(f => f.TargetsToBuild).ToList();
            Assert.AreEqual(2, task2Targets.Count, "we expect 2 targets");
            Assert.IsTrue(task2Targets.Exists(t => t.GetTargetPath().Equals($"{baseDir}\\archive.zip\\newfile1")));
            Assert.IsTrue(task2Targets.Exists(t => t.GetTargetPath().Equals($"{baseDir}\\archive.zip\\newfile2.ext")));
        }
        
        private class TaskOnArchive : AOeTaskFileArchiverArchive {
            public IArchiver Archiver { get; set; }
            public override string TargetArchivePath { get; set; }
            public override string TargetFilePath { get; set; }
            public override string TargetDirectory { get; set; }
            protected override IArchiver GetArchiver() => Archiver;
            protected override AOeTarget GetNewTarget() => new OeTargetZip();
            public List<IOeFileToBuild> Files { get; set; } = new List<IOeFileToBuild>();
            protected override void ExecuteInternalArchive() {
                Files.AddRange(GetFilesToBuild());
            }
        }
        
        private class TaskSetPathAndTargetTest : AOeTaskFilter, IOeTaskFile {
            public int Total { get; set; }
            protected override void ExecuteInternal() {
                // does nothing
            }
            protected override void ExecuteTestModeInternal() => throw new NotImplementedException();
            public void SetFilesToProcess(PathList<IOeFile> pathsToBuild) => Total += 4;
            public PathList<IOeFile> GetFilesToProcessFromIncludes() {
                Total += 8;
                return new PathList<IOeFile>();
            }
            public PathList<IOeFile> GetFilesToProcess() => throw new NotImplementedException();
            public void ValidateCanGetFilesToProcessFromIncludes() => throw new NotImplementedException();
        }
        
        private class TaskSetPathAndTargetTest2 : AOeTaskFilter, IOeTaskDirectory {
            public int Total { get; set; }
            protected override void ExecuteInternal() {
                // does nothing
            }
            protected override void ExecuteTestModeInternal() => throw new NotImplementedException();
            public void SetDirectoriesToProcess(PathList<IOeDirectory> pathsToBuild) => Total += 1;
            public PathList<IOeDirectory> GetDirectoriesToProcessFromIncludes() {
                Total += 2;
                return new PathList<IOeDirectory>();
            }
            public PathList<IOeDirectory> GetDirectoriesToProcess() => throw new NotImplementedException();
            public void ValidateCanGetDirectoriesToProcessFromIncludes() => throw new NotImplementedException();
        }
        
        private class TaskInjectionTest : AOeTaskFile, IOeTaskCompile {
            public bool IsLogSet => Log != null;
            public bool IsCancelSourceSet => CancelToken != null;
            public bool IsTestSet => TestMode;
            public bool IsFilesCompiledSet { get; private set; }
            public bool IsBaseDirectorySet { get; private set; }
            public bool IsPropertySet => GetProperties() != null;
            public void SetCompiledFiles(PathList<UoeCompiledFile> compiledPath) {
                IsFilesCompiledSet = true;
            }
            public void SetTargetBaseDirectory(string baseDirectory) {
                IsBaseDirectorySet = true;
            }
            public PathList<UoeCompiledFile> GetCompiledFiles() => null;
            protected override void ExecuteInternal() {}
            protected override void ExecuteTestModeInternal() { }

            public PathList<IOeFileToBuild> GetFilesToBuild() {
                return null;
            }
            public void SetTargets(PathList<IOeFileToBuild> paths, string baseTargetDirectory, bool appendMode = false) { }
        }
        
        private class TaskException : AOeTask {
            public override void Validate() { }
            protected override void ExecuteInternal() {
                AddExecutionErrorAndThrow(new TaskExecutionException(this, "oups!"));
            }
            protected override void ExecuteTestModeInternal() {
                // nothing to do
            }
        }
        
        private class TaskWarning : AOeTask {
            public override void Validate() { }
            protected override void ExecuteInternal() {
                AddExecutionWarning(new TaskExecutionException(this, "oups warning!"));
            }
            protected override void ExecuteTestModeInternal() {
                // nothing to do
            }
        }
        
        private class TaskWaitForCancel : IOeTask {
            private CancellationToken? _cancelToken;
            public void Validate() {}

            public void Execute() {
                _cancelToken?.WaitHandle.WaitOne();
                _cancelToken?.ThrowIfCancellationRequested();
            }
            public void SetLog(ILogger log) {}
            public void SetTestMode(bool testMode) { }
            public event EventHandler<TaskWarningEventArgs> PublishWarning;
            public void SetCancelToken(CancellationToken? cancelToken) {
                _cancelToken = cancelToken;
                PublishWarning?.Invoke(null, null);
            }
            public List<TaskExecutionException> GetRuntimeExceptionList() => null;
        }

    }
}