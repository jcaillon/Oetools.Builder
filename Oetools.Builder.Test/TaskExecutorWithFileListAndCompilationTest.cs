#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutorWithFileListAndCompilationTest.cs) is part of Oetools.Builder.Test.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Test {
    
    [TestClass]
    public class TaskExecutorWithFileListAndCompilationTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(TaskExecutorWithFileListAndCompilationTest)));
                     
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
        public void TaskExecutorWithFileListAndCompilation_Test_compile_failed_or_warning() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }

            var sourceDir = Path.Combine(TestFolder, "source2");
            Utils.CreateDirectoryIfNeeded(sourceDir);
            
            var taskFiles = new List<OeFile> {
                new OeFile { SourceFilePath = Path.Combine(sourceDir, "file1.p") },
                new OeFile { SourceFilePath = Path.Combine(sourceDir, "file2.w") },
                new OeFile { SourceFilePath = Path.Combine(sourceDir, "file3.ext") }
            };
            var taskExecutor = new TaskExecutorWithFileListAndCompilation {
                TaskFiles = taskFiles,
                OutputDirectory = Path.Combine(TestFolder, "source2", "bin"),
                SourceDirectory = sourceDir,
                ProjectProperties = new OeProjectProperties(),
                Env = new UoeExecutionEnv {
                    UseProgressCharacterMode = true
                }
            };

            File.WriteAllText(Path.Combine(sourceDir, "file1.p"), "Quit.");
            File.WriteAllText(Path.Combine(sourceDir, "file2.w"), "derp.");

            var taskCompile = new TaskCompile { Include = @"**", TargetDirectory = @"" };
            
            taskExecutor.Tasks = new List<IOeTask> { taskCompile };

            taskExecutor.ProjectProperties.TreatWarningsAsErrors = true;
            taskExecutor.ProjectProperties.StopBuildOnCompilationError = true;

            Assert.ThrowsException<TaskExecutorException>(() => taskExecutor.Execute());
            
            Assert.AreEqual(2, taskExecutor.CompiledFiles.Count, "2 files compiled");
            Assert.AreEqual(1, taskExecutor.CompilerNumberOfProcessesUsed, "1 process used");
            Assert.AreEqual(0, taskExecutor.CompilerHandledExceptions?.Count ?? 0, "no exceptions, we just have compilations problems but the compiler itself is OK");
            Assert.AreEqual(2, taskExecutor.CompiledFiles.Where(cp => cp.CompilationErrors != null).SelectMany(cp => cp.CompilationErrors).Count(), "we should find 2 compilation errors (2 for the same file file2.w)");
            
            taskExecutor.ProjectProperties.TreatWarningsAsErrors = true;
            taskExecutor.ProjectProperties.StopBuildOnCompilationError = false;

            // this should not throw exception since we don't want the build to stop on compilation error
            taskExecutor.Execute();
            
            Assert.AreEqual(1, taskCompile.Files.Count, "only file1.r will be copied");
            Assert.AreEqual(1, taskExecutor.CompiledFiles.Count(cf => cf.CompiledCorrectly), "we should have only one file compiled correctly");
            var taskTargets = taskCompile.Files.SelectMany(f => f.TargetsFiles).ToList();
            Assert.AreEqual(1, taskTargets.Count, "we expect only 1 target because the file that didn't compile was no included");
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetFilePath().Equals(Path.Combine(TestFolder, "source2", "bin", @"file1.r"))));
            
            taskExecutor.ProjectProperties.TreatWarningsAsErrors = false;
            taskExecutor.ProjectProperties.StopBuildOnCompilationError = true;
            
            File.WriteAllText(Path.Combine(sourceDir, "file2.w"), "quit. quit.");
            taskCompile.Files.Clear();
            
            // we stop on errors but we do not consider warnings as error, so we should be ok w/o exceptions
            taskExecutor.Execute();
            
            Assert.AreEqual(2, taskCompile.Files.Count, "both files will be copied");
            Assert.AreEqual(1, taskExecutor.CompiledFiles.Count(cf => cf.CompiledCorrectly), "we should have only one file compiled correctly");
            Assert.AreEqual(1, taskExecutor.CompiledFiles.Count(cf => cf.CompiledWithWarnings), "and one with warning");
            taskTargets = taskCompile.Files.SelectMany(f => f.TargetsFiles).ToList();
            Assert.AreEqual(2, taskTargets.Count, "we expect 2 targets here");
            
            taskExecutor.ProjectProperties.TreatWarningsAsErrors = true;
            taskExecutor.ProjectProperties.StopBuildOnCompilationError = true;
            
            File.WriteAllText(Path.Combine(sourceDir, "file2.w"), "quit. quit.");

            // now we consider warnings as errors and we stop on errors
            Assert.ThrowsException<TaskExecutorException>(() => taskExecutor.Execute());
            
            Assert.AreEqual(1, taskExecutor.CompiledFiles.Count(cf => cf.CompiledCorrectly), "we should have only one file compiled correctly");
            Assert.AreEqual(1, taskExecutor.CompiledFiles.Count(cf => cf.CompiledWithWarnings), "and one with warning");
            taskTargets = taskCompile.Files.SelectMany(f => f.TargetsFiles).ToList();
            Assert.AreEqual(2, taskTargets.Count, "we expect 2 targets here");
        }
        
                
        [TestMethod]
        public void TaskExecutorWithFileListAndCompilation_Test_compile() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }

            var sourceDir = Path.Combine(TestFolder, "source");
            Utils.CreateDirectoryIfNeeded(sourceDir);
            
            var taskExecutor = new TaskExecutorWithFileListAndCompilation {
                TaskFiles = new List<OeFile> {
                    new OeFile { SourceFilePath = Path.Combine(sourceDir, "file1.p") },
                    new OeFile { SourceFilePath = Path.Combine(sourceDir, "file2.w") },
                    new OeFile { SourceFilePath = Path.Combine(sourceDir, "file3.ext") }
                },
                OutputDirectory = Path.Combine(TestFolder, "source", "bin"),
                SourceDirectory = sourceDir,
                ProjectProperties = new OeProjectProperties(),
                Env = new UoeExecutionEnv {
                    UseProgressCharacterMode = true
                }
            };

            foreach (var taskFile in taskExecutor.TaskFiles) {
                File.WriteAllText(taskFile.SourceFilePath, "Quit.");
            }

            var taskCompile = new TaskCompile {
                Include = @"**",
                TargetDirectory = @""
            };
            
            taskExecutor.Tasks = new List<IOeTask> { taskCompile };
            
            taskExecutor.Execute();
            
            Assert.AreEqual(2, taskExecutor.CompiledFiles.Count, "2 files compiled");
            Assert.AreEqual(1, taskExecutor.CompilerNumberOfProcessesUsed, "1 process used");
            Assert.AreEqual(0, taskExecutor.CompilerHandledExceptions?.Count ?? 0, "no exceptions");

            Assert.AreEqual(2, taskCompile.Files.Count, "only file1.p and file2.w were included");
            var taskTargets = taskCompile.Files.SelectMany(f => f.TargetsFiles).ToList();
            Assert.AreEqual(2, taskTargets.Count, "we expect 2 targets");
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetFilePath().Equals(Path.Combine(TestFolder, "source", "bin", "file1.r"))));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetFilePath().Equals(Path.Combine(TestFolder, "source", "bin", "file2.r"))));
        }
        
        private class TaskCompile : OeTaskFileTargetFile, IOeTaskCompile {
            public List<IOeFileToBuildTargetFile> Files { get; set; } = new List<IOeFileToBuildTargetFile>();
            protected override void ExecuteForFilesInternal(IEnumerable<IOeFileToBuildTargetFile> files) {
                Files.AddRange(files);
            }
        }

        [TestMethod]
        public void TaskExecutorWithFileListAndCompilation_Test_GetFilesToCompile_simple() {
            var tasks = new List<IOeTask> {
                new TaskFilterCompile {
                    Include = @"**/folder/**",
                    Exclude = @"**filtered**"
                },
                new TaskFilterCompile {
                    Include = @"**/new/**",
                    Exclude = @"**filtered**"
                },
                new TaskFilterCompile {
                    Include = @"**.w"
                },
                // the filter below should be ignored
                new OeTaskFilter {
                    Include = @"**"
                },
                // the filter below should be ignored
                new OeTaskFilter {
                    Include = @"/new/file3.t"
                }
            };
            var files = new List<OeFile> {
                new OeFile(@"/folder/file1.p") { Size = 10 },
                new OeFile(@"/folder/file2.cls"),
                new OeFile(@"/new/file3.t"),
                new OeFile(@"/new/file4.w"),
                new OeFile(@"/new/file5.random"),
                new OeFile(@"/new/file6.notoe"),
                new OeFile(@"/filtered/file1.p"),
                new OeFile(@"/filtered/file4.w"),
                new OeFile(@"file7.p")
            };
            
            
            var filesToCompile = TaskExecutorWithFileListAndCompilation.GetFilesToCompile(tasks, files.Where(f => f.SourceFilePath.TestFileNameAgainstListOfPatterns(OeCompilationOptions.GetDefaultCompilableFilePattern())));
            
            Assert.AreEqual(5, filesToCompile.Count);
            Assert.IsTrue(filesToCompile.Exists(f => f.FileSize.Equals(10)), "should preserve file size");
            Assert.IsTrue(filesToCompile.Exists(f => f.SourcePath.Equals(@"/folder/file1.p")));
            Assert.IsTrue(filesToCompile.Exists(f => f.SourcePath.Equals(@"/folder/file2.cls")));
            Assert.IsTrue(filesToCompile.Exists(f => f.SourcePath.Equals(@"/new/file3.t")));
            Assert.IsTrue(filesToCompile.Exists(f => f.SourcePath.Equals(@"/new/file4.w")));
            Assert.IsTrue(filesToCompile.Exists(f => f.SourcePath.Equals(@"/filtered/file4.w")));
        }

        [TestMethod]
        public void TaskExecutorWithFileListAndCompilation_Test_GetFilesToCompile_complex() {
            // TODO : test the second version of GetFilesToCompile
        }
        
        private class TaskFilterCompile : OeTaskFilter, IOeTaskCompile { }
    }
}