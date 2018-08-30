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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Utilities.Lib;
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
            
            var taskFiles = new FileList<OeFile> {
                new OeFile { FilePath = Path.Combine(sourceDir, "file1.p") },
                new OeFile { FilePath = Path.Combine(sourceDir, "file2.w") },
                new OeFile { FilePath = Path.Combine(sourceDir, "file3.ext") }
            };
            var taskExecutor = new BuildStepExecutorWithFileListAndCompilation {
                TaskFiles = taskFiles,
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        OutputDirectoryPath = Path.Combine(TestFolder, "source2", "bin"),
                        SourceDirectoryPath = sourceDir,
                    }
                }
            };

            File.WriteAllText(Path.Combine(sourceDir, "file1.p"), "Quit.");
            File.WriteAllText(Path.Combine(sourceDir, "file2.w"), "derp.");

            var taskCompile = new TaskCompile { Include = @"**", TargetDirectory = @"" };
            
            taskExecutor.Tasks = new List<IOeTask> { taskCompile };

            taskExecutor.Properties.BuildOptions.StopBuildOnCompilationWarning = true;
            taskExecutor.Properties.BuildOptions.StopBuildOnCompilationError = true;

            Exception ex = null;
            try {
                taskExecutor.Execute();
            } catch (Exception e) {
                ex = e;
            }
            Assert.IsNotNull(ex);
            Assert.AreEqual(null, taskCompile.GetCompiledFiles(), "build failed we don't get results");
            Assert.AreEqual(typeof(CompilerException), ex.InnerException.GetType());
            
            taskExecutor.Properties.BuildOptions.StopBuildOnCompilationWarning = false;
            taskExecutor.Properties.BuildOptions.StopBuildOnCompilationError = false;

            // this should not throw exception since we don't want the build to stop on compilation error
            taskExecutor.Execute();
            
            Assert.AreEqual(1, taskCompile.Files.Count, "only file1.r will be copied");
            Assert.AreEqual(1, taskCompile.GetCompiledFiles().Count(cf => cf.CompiledCorrectly), "we should have only one file compiled correctly");
            var taskTargets = taskCompile.Files.SelectMany(f => f.TargetsFiles).ToList();
            Assert.AreEqual(1, taskTargets.Count, "we expect only 1 target because the file that didn't compile was no included");
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(TestFolder, "source2", "bin", @"file1.r"))));
            
            taskExecutor.Properties.BuildOptions.StopBuildOnCompilationWarning = false;
            taskExecutor.Properties.BuildOptions.StopBuildOnCompilationError = true;
            
            File.WriteAllText(Path.Combine(sourceDir, "file2.w"), "quit. quit.");
            taskCompile.Files.Clear();
            
            // we stop on errors but not on warnings so it's ok
            taskExecutor.Execute();
            
            Assert.AreEqual(2, taskCompile.Files.Count, "both files will be copied");
            Assert.AreEqual(1, taskCompile.GetCompiledFiles().Count(cf => cf.CompiledCorrectly), "we should have only one file compiled correctly");
            Assert.AreEqual(1, taskCompile.GetCompiledFiles().Count(cf => cf.CompiledWithWarnings), "and one with warning");
            taskTargets = taskCompile.Files.SelectMany(f => f.TargetsFiles).ToList();
            Assert.AreEqual(2, taskTargets.Count, "we expect 2 targets here");
            
            taskExecutor.Properties.BuildOptions.StopBuildOnCompilationWarning = true;
            taskExecutor.Properties.BuildOptions.StopBuildOnCompilationError = true;
            
            // now we consider warnings as errors
            ex = null;
            try {
                taskExecutor.Execute();
            } catch (Exception e) {
                ex = e;
            }
            Assert.IsNotNull(ex);
        }
        
                
        [TestMethod]
        public void TaskExecutorWithFileListAndCompilation_Test_compile() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }

            var sourceDir = Path.Combine(TestFolder, "source");
            Utils.CreateDirectoryIfNeeded(sourceDir);
            
            var taskExecutor = new BuildStepExecutorWithFileListAndCompilation {
                TaskFiles = new FileList<OeFile> {
                    new OeFile { FilePath = Path.Combine(sourceDir, "file1.p") },
                    new OeFile { FilePath = Path.Combine(sourceDir, "file2.w") },
                    new OeFile { FilePath = Path.Combine(sourceDir, "file3.ext") }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDir,
                        OutputDirectoryPath = Path.Combine(TestFolder, "source", "bin")
                    }
                }
            };

            foreach (var taskFile in taskExecutor.TaskFiles) {
                File.WriteAllText(taskFile.FilePath, "Quit.");
            }

            var taskCompile = new TaskCompile {
                Include = @"**",
                TargetDirectory = @""
            };
            
            taskExecutor.Tasks = new List<IOeTask> { taskCompile };
            
            taskExecutor.Execute();
            
            Assert.AreEqual(2, taskCompile.GetCompiledFiles().Count, "2 files compiled");

            Assert.AreEqual(2, taskCompile.Files.Count, "file1.p and file2.w were included");
            var taskTargets = taskCompile.Files.SelectMany(f => f.TargetsFiles).ToList();
            Assert.AreEqual(2, taskTargets.Count, "we expect 2 targets");
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(TestFolder, "source", "bin", "file1.r"))));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(TestFolder, "source", "bin", "file2.r"))));
        }
        
        private class TaskCompile : OeTaskFileTargetFile, IOeTaskCompile {
            public List<IOeFileToBuildTargetFile> Files { get; } = new List<IOeFileToBuildTargetFile>();
            public override void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files) {
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
            var files = new FileList<OeFile> {
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

            // equivalent to property injection done in the task executor
            foreach (var task in tasks.Where(t => t is IOeTaskCompile).Cast<IOeTaskCompile>()) {
                task.SetFileExtensionFilter(OeCompilationOptions.GetDefaultCompilableFileExtensionPattern());
            }
            
            var filesToCompile = BuildStepExecutorWithFileListAndCompilation.GetFilesToCompile(tasks, files);
            
            Assert.AreEqual(5, filesToCompile.Count);
            Assert.IsTrue(filesToCompile.ToList().Exists(f => f.FileSize.Equals(10)), "should preserve file size");
            Assert.IsTrue(filesToCompile.ToList().Exists(f => f.FilePath.Equals(@"/folder/file1.p")));
            Assert.IsTrue(filesToCompile.ToList().Exists(f => f.FilePath.Equals(@"/folder/file2.cls")));
            Assert.IsTrue(filesToCompile.ToList().Exists(f => f.FilePath.Equals(@"/new/file3.t")));
            Assert.IsTrue(filesToCompile.ToList().Exists(f => f.FilePath.Equals(@"/new/file4.w")));
            Assert.IsTrue(filesToCompile.ToList().Exists(f => f.FilePath.Equals(@"/filtered/file4.w")));
        }

        [TestMethod]
        public void TaskExecutorWithFileListAndCompilation_Test_GetFilesToCompile_complex() {
            // TODO : test the second version of GetFilesToCompile
        }

        private class TaskFilterCompile : OeTaskFilter, IOeTaskCompile {
            public void SetCompiledFiles(FileList<UoeCompiledFile> compiledFile) { CompiledFiles = compiledFile; }
            public FileList<UoeCompiledFile> GetCompiledFiles() => CompiledFiles;
            private FileList<UoeCompiledFile> CompiledFiles { get; set; }
        }
    }
}