#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (BuilderTest.cs) is part of Oetools.Builder.Test.
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
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Project.Task;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Test {
    
    [TestClass]
    public class BuilderTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(BuilderTest)));
                     
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
        public void Builder_Test_TestMode_FullRebuild_TargetRemover() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var outputDirectory = Path.Combine(TestFolder, "source_build_output");
            var sourceDirectory = Path.Combine(TestFolder, "source_build");
            Utils.CreateDirectoryIfNeeded(Path.Combine(sourceDirectory, "subfolder"));
            
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "quit."); // compile ok
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "quit. quit."); // compile with warnings
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file4.p"), "quit.");
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file5.p"), "quit.");
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "resource.ext"), "ok");
            
            var buildConfiguration1 = new OeBuildConfiguration {
                BuildSteps = new List<AOeBuildStep> {
                    new OeBuildStepBuildSource {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileCompile { Include = "**/subfolder/**", TargetDirectory = "subfolder" },
                            new OeTaskFileArchiverArchiveProlib { Include = "**", TargetArchivePath = "w.pl", TargetDirectory = "screens" }
                        }
                    },
                    new OeBuildStepBuildSource {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileCompile { Exclude = "**/subfolder/**", TargetDirectory = "root" },
                            new OeTaskFileCopy { Include = "**.ext", TargetFilePath = "resources/file.new" }
                        }
                    }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDirectory,
                        OutputDirectoryPath = outputDirectory,
                        StopBuildOnTaskWarning = true,
                        StopBuildOnCompilationError = false,
                        StopBuildOnCompilationWarning = false,
                        IncrementalBuildOptions = new OeIncrementalBuildOptions {
                            EnabledIncrementalBuild = true,
                            UseCheckSumComparison = true,
                            RebuildFilesWithCompilationErrors = true
                        }
                    }
                }
            };

            OeBuildHistory firstBuildHistory;
            
            using (var builder = new Builder(buildConfiguration1)) {
                builder.Build();
                Assert.AreEqual(5, builder.GetAllFilesBuilt().Count, "we expect to have 5 files built");
                Assert.AreEqual(10, builder.GetAllFilesBuilt().SelectMany(f => f.Targets.ToNonNullEnumerable()).Count(), "we expect to have 10 targets");
                firstBuildHistory = builder.BuildSourceHistory.GetDeepCopy();
            }

            
            // we now rebuild this project, injecting the previous history
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = firstBuildHistory
            }) {
                builder.Build();
                Assert.AreEqual(0, builder.GetAllFilesBuilt().Count, "we expect to have 0 files built this time, nothing has changed");
            }

            
            // full rebuild 
            buildConfiguration1.Properties.BuildOptions.FullRebuild = true;
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = firstBuildHistory
            }) {
                builder.Build();
                Assert.AreEqual(5, builder.GetAllFilesBuilt().Count, "we expect to have 5 files built");
                Assert.AreEqual(10, builder.GetAllFilesBuilt().SelectMany(f => f.Targets.ToNonNullEnumerable()).Count(), "we expect to have 10 targets");
            }
            buildConfiguration1.Properties.BuildOptions.FullRebuild = false;
            
            
            // delete a file
            File.Delete(Path.Combine(sourceDirectory, "file2.w"));
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = firstBuildHistory
            }) {
                builder.Build();
                Assert.AreEqual(0, builder.GetAllFilesBuilt().Count);
                Assert.AreEqual(0, builder.GetAllFilesWithTargetRemoved().Count);
            }
            
            buildConfiguration1.BuildSteps.Last().Tasks.Add(new OeTaskReflectDeletedSourceFile());
            OeBuildHistory lastBuildHistory;
            
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = firstBuildHistory
            }) {
                builder.Build();
                Assert.AreEqual(1, builder.GetAllFilesWithTargetRemoved().Count);
                Assert.AreEqual(2, builder.GetAllFilesWithTargetRemoved().SelectMany(f => f.Targets.ToNonNullEnumerable()).Count());
                
                lastBuildHistory = builder.BuildSourceHistory.GetDeepCopy();
            }
            
            
            // remove the second task = remove targets
            buildConfiguration1.BuildSteps[0].Tasks.RemoveAt(1);
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = lastBuildHistory
            }) {
                builder.Build();
                Assert.AreEqual(0, builder.GetAllFilesBuilt().Count);
                Assert.AreEqual(0, builder.GetAllFilesWithTargetRemoved().Count);
            }
            buildConfiguration1.BuildSteps.Last().Tasks.Add(new OeTaskReflectDeletedTargets());
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = lastBuildHistory
            }) {
                builder.Build();
                Assert.AreEqual(4, builder.GetAllFilesWithTargetRemoved().Count);
                Assert.AreEqual(4, builder.GetAllFilesWithTargetRemoved().SelectMany(f => f.Targets.ToNonNullEnumerable()).Count());
                
                lastBuildHistory = builder.BuildSourceHistory.GetDeepCopy();
            }
            
            
            // replace root target by new + modify a file
            ((OeTaskFileCompile) buildConfiguration1.BuildSteps[1].Tasks[0]).TargetDirectory = "new";
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = lastBuildHistory
            }) {
                builder.Build();
                Assert.AreEqual(0, builder.GetAllFilesBuilt().Count);
                Assert.AreEqual(1, builder.GetAllFilesWithTargetRemoved().Count);
                Assert.AreEqual(1, builder.GetAllFilesWithTargetRemoved().SelectMany(f => f.Targets.ToNonNullEnumerable()).Count());
            }
            buildConfiguration1.Properties.BuildOptions.IncrementalBuildOptions.RebuildFilesWithNewTargets = true;
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = lastBuildHistory
            }) {
                builder.Build();
                Assert.AreEqual(1, builder.GetAllFilesBuilt().Count);
                Assert.AreEqual(1, builder.GetAllFilesBuilt().SelectMany(f => f.Targets.ToNonNullEnumerable()).Count());
                Assert.AreEqual(1, builder.GetAllFilesWithTargetRemoved().Count);
                Assert.AreEqual(1, builder.GetAllFilesWithTargetRemoved().SelectMany(f => f.Targets.ToNonNullEnumerable()).Count());
                
                lastBuildHistory = builder.BuildSourceHistory.GetDeepCopy();
            }

        }
        
        [TestMethod]
        public void Builder_Test_Build_Free_tasks() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var sourceDirectory = Path.Combine(TestFolder, "source_test_pre_post");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "");
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "");
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.ext"), "");
            
            var builder = new Builder(new OeBuildConfiguration {
                BuildSteps = new List<AOeBuildStep> {
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileTargetFileCopy2 { Include = "{{SOURCE_DIRECTORY}}/**.w", TargetDirectory = "{{SOURCE_DIRECTORY}}/copied_w" },
                            new OeTaskFileTargetArchiveProlibCompileProlib2 { Include = "{{SOURCE_DIRECTORY}}/**file1**", TargetArchivePath = "{{SOURCE_DIRECTORY}}/my.pl", TargetDirectory = "" }
                        }
                    },
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileTargetFileCopy2 { Include = "{{SOURCE_DIRECTORY}}/**", Exclude = "**((.p||.w))", TargetDirectory = "{{SOURCE_DIRECTORY}}/copied_ext" }
                        }
                    }  
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDirectory,
                        OutputDirectoryPath = sourceDirectory,
                        StopBuildOnTaskWarning = true
                    }
                }
            });
            
            builder.Build();
            
            var filesBuilt = builder.BuildStepExecutors
                .SelectMany(exec => exec?.Tasks)
                .OfType<IOeTaskWithBuiltFiles>()
                .SelectMany(t => t.GetBuiltFiles().ToNonNullEnumerable())
                .ToList();
            
            Assert.AreEqual(1, ((IOeTaskCompile) builder.BuildStepExecutors[0].Tasks.ToList()[1]).GetCompiledFiles().Count);
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "copied_w", "file2.w"), filesBuilt[0].Targets[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "my.pl", "file1.r"), filesBuilt[1].Targets[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "copied_ext", "file3.ext"), filesBuilt[2].Targets[0].GetTargetPath());
        }

        [TestMethod]
        public void Builder_Test_Build_output() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var sourceDirectory = Path.Combine(TestFolder, "source_test_build_output");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "");
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "");
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.ext"), "");
            
            var builder = new Builder(new OeBuildConfiguration {
                BuildSteps = new List<AOeBuildStep> {
                    new OeBuildStepBuildOutput {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileTargetFileCopy2 { Include = "**.w", TargetDirectory = "copied_w" },
                            new OeTaskFileTargetArchiveProlibCompileProlib2 { Include = "**file1**", TargetArchivePath = "my.pl", TargetDirectory = "" }
                        }
                    },
                    new OeBuildStepBuildOutput {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileTargetFileCopy2 { Include = "**", Exclude = "**((.p||.w))", TargetDirectory = "copied_ext" }
                        }
                    }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDirectory,
                        OutputDirectoryPath = sourceDirectory
                    }
                }
            });
            
            builder.Build();

            var filesBuilt = builder.GetAllFilesBuilt();
            
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "copied_w", "file2.w"), filesBuilt[0].Targets[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "my.pl", "file1.r"), filesBuilt[1].Targets[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "copied_ext", "file3.ext"), filesBuilt[2].Targets[0].GetTargetPath());
        }

        [TestMethod]
        public void Builder_Test_Source_History() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var sourceDirectory = Path.Combine(TestFolder, "source_test_history");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "{file1.i}"); // compile ok
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.i"), "quit.");
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "quit. quit."); // compile with warnings
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.p"), "nof sense, will not compile"); // compile with errors


            var builder = new Builder(new OeBuildConfiguration {
                BuildSteps = new List<AOeBuildStep> {
                    new OeBuildStepBuildSource {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**file1**", TargetDirectory = "first;/random/folder" },
                            new OeTaskFileTargetArchiveProlibCompileProlib2 { Include = "**file((2||3))**", TargetArchivePath = "my.pl", TargetDirectory = "" }
                        }
                    },
                    new OeBuildStepBuildSource {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**file1**", TargetDirectory = "second" }
                        }
                    }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDirectory,
                        StopBuildOnCompilationError = false,
                        StopBuildOnCompilationWarning = false,
                        IncrementalBuildOptions = new OeIncrementalBuildOptions {
                            EnabledIncrementalBuild = true,
                            UseCheckSumComparison = true
                        }
                    }
                }
            }) {
                BuildSourceHistory = new OeBuildHistory {
                    BuiltFiles = new List<OeFileBuilt> {
                        new OeFileBuilt {
                            Path = "myfile.p",
                            Size = 2,
                            State = OeFileState.Modified,
                            Targets = new List<AOeTarget> {
                                new OeTargetFile {
                                    FilePathInArchive = "derp.out.p"
                                }
                            },
                            Checksum = "okay"
                        }
                    }
                }
            };
            Assert.AreEqual(true, builder.BuildConfiguration.Properties.BuildOptions.IncrementalBuildOptions.EnabledIncrementalBuild);

            builder.Build();
            
            Assert.AreEqual(4, builder.BuildSourceHistory.BuiltFiles.Count);
            
            // check each file in history
            foreach (var file in builder.BuildSourceHistory.BuiltFiles) {
                Assert.IsFalse(string.IsNullOrEmpty(file.Checksum));
                Assert.AreEqual(OeFileState.Added, file.State);
                Assert.IsTrue(file.Size > 0);
            }

            // files built
            var file1 = builder.BuildSourceHistory.BuiltFiles[0];
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "first", "file1.r"), file1.Targets.ToList()[0].GetTargetPath());
            Assert.AreEqual(Path.Combine("C:\\random\\folder", "file1.r"), file1.Targets.ToList()[1].GetTargetPath());
            var file2 = builder.BuildSourceHistory.BuiltFiles[1];
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "my.pl", "file2.r"), file2.Targets.ToList()[0].GetTargetPath());
            
            // compilation problems
            Assert.AreEqual(1, file2.CompilationProblems.Count);
            var file3 = builder.BuildSourceHistory.BuiltFiles[2];
            Assert.AreEqual(2, file3.CompilationProblems.Count);
            
            // files required
            var file4 = builder.BuildSourceHistory.BuiltFiles[3];
            Assert.IsTrue(file4.Path.EndsWith("file1.i"));
            Assert.AreEqual(5, file4.Size);
            
            builder.Dispose();        
            
        }

        [TestMethod]
        public void Builder_Test_Compilation_problems() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var sourceDirectory = Path.Combine(TestFolder, "source_test_compilation_problems");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "quit."); // compile ok
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "quit. quit."); // compile with warnings
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.p"), "{inc4.i}"); // compile with errors
            File.WriteAllText(Path.Combine(sourceDirectory, "inc4.i"), "nof sense, will not compile"); // compile with errors

            var builder = new Builder(new OeBuildConfiguration {
                BuildSteps = new List<AOeBuildStep> {
                    new OeBuildStepBuildSource {
                        Tasks = new List<AOeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**", TargetDirectory = "" }
                        }
                    }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        StopBuildOnCompilationError = false,
                        StopBuildOnCompilationWarning = false,
                        SourceDirectoryPath = sourceDirectory
                    }
                }
            });

            builder.Build();
            
            Assert.AreEqual(3, builder.CompilationProblems.Count, "3 compilation problems.");
            Assert.AreEqual(1, builder.CompilationProblems.Count(p => p is OeCompilationWarning));
            
            Assert.AreEqual(4, builder.BuildSourceHistory.BuiltFiles.Count, "4 files in history.");
            Assert.AreEqual(2, builder.BuildSourceHistory.BuiltFiles.SelectMany(f => f.Targets.ToNonNullEnumerable()).Count(), "only two targets because only 2 files compiled.");

            builder.Dispose();        
            
        }

        private class OeTaskFileTargetArchiveProlibCompileProlib2 : OeTaskFileArchiverArchiveProlibCompile {
            protected override void ExecuteInternalArchive() {
                ExecuteTestModeInternal();
            }
        }

        private class OeTaskFileTargetFileCompile2 : OeTaskFileCompile {
            protected override void ExecuteInternalArchive() {
                ExecuteTestModeInternal();
            }
        }
        
        private class OeTaskFileTargetFileCopy2 : OeTaskFileCopy {
            protected override void ExecuteInternalArchive() {
                ExecuteTestModeInternal();
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Builder_Constructor_set_default_values(bool useProject) {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            Builder builder;
            if (useProject) {
                builder = new Builder(new OeProject());
            } else {
                builder = new Builder(new OeBuildConfiguration());
            }
            Assert.AreEqual(OeProperties.GetDefaultAddDefaultOpenedgePropath(), builder.BuildConfiguration.Properties.AddDefaultOpenedgePropath);
            Assert.AreEqual(OeBuildOptions.GetDefaultStopBuildOnCompilationError(), builder.BuildConfiguration.Properties.BuildOptions.StopBuildOnCompilationError);
            Assert.IsFalse(string.IsNullOrEmpty(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath));
        }

        /// <summary>
        /// Tests that we get what we need in <see cref="Builder.BuildStepExecutors"/>.
        /// </summary>
        [TestMethod]
        public void Builder_Test_All_Task_build_steps() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var builder = new Builder(new OeBuildConfiguration {
                BuildSteps = new List<AOeBuildStep> {
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepBuildSource {
                        Tasks = new List<AOeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepBuildSource {
                        Tasks = new List<AOeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new OeTaskExec2()
                        }
                    }
                }
            });

            builder.Build();
            
            Assert.AreEqual(8, builder.BuildStepExecutors.Count, "1 per build step, 2 step group times 4");
            Assert.IsTrue(builder.BuildStepExecutors.SelectMany(bse => bse.Tasks).All(t => ((OeTaskExec2)t).Count == 1));
                
            builder.Dispose();
        }

        private class OeTaskExec2 : OeTaskExec {
            public int Count { get; set; }
            protected override void ExecuteInternal() {
                Count++;
            }
            public override void Validate() { }
        }
        
        [TestMethod]
        public void Builder_Test_Cancel() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var cancel = new CancellationTokenSource();
            var builder = new Builder(new OeBuildConfiguration {
                BuildSteps = new List<AOeBuildStep> {
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new TaskWaitForCancel()
                        }
                    }
                }
            }) {
                CancelToken = cancel.Token
            };
            
            Task.Factory.StartNew(() => {
                Thread.Sleep(1000);
                cancel.Cancel();
            });

            Exception ex = null;
            try {
                builder.Build();
            } catch (BuilderException e) {
                ex = e;
            }
            Assert.IsNotNull(ex);
            Assert.AreEqual(typeof(OperationCanceledException), ex.InnerException.GetType());
        }

        private class TaskWaitForCancel : AOeTask {
            public override void Validate() {
                // nothing to validate
            }

            protected override void ExecuteInternal() {
                Log?.Debug("");
                CancelToken?.WaitHandle.WaitOne();
                CancelToken?.ThrowIfCancellationRequested();
            }

            protected override void ExecuteTestModeInternal() {
                // nothing should happen
            }
        }
        
        [TestMethod]
        public void Builder_Test_Stop() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var conf = new OeBuildConfiguration {
                BuildSteps = new List<AOeBuildStep> {
                    new OeBuildStepFree {
                        Tasks = new List<AOeTask> {
                            new TaskThrowWarningsAndErrors(),
                            new TaskThrowWarningsAndErrors()
                        }
                    }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        StopBuildOnTaskError = true,
                        StopBuildOnTaskWarning = true
                    }
                }
            };

            Exception ex = null;
            
            using (var builder = new Builder(conf)) {
                try {
                    builder.Build();
                } catch (BuilderException e) {
                    ex = e;
                }
                Assert.IsNotNull(ex);
                Assert.AreEqual(1, builder.TaskExecutionExceptions.Count);
                Assert.IsTrue( builder.TaskExecutionExceptions[0].IsWarning);
            }



            conf.Properties.BuildOptions.StopBuildOnTaskWarning = false;

            using (var builder = new Builder(conf)) {
                try {
                    builder.Build();
                } catch (BuilderException e) {
                    ex = e;
                }
                Assert.IsNotNull(ex);
                Assert.AreEqual(2, builder.TaskExecutionExceptions.Count);
                Assert.IsFalse(builder.TaskExecutionExceptions.Last().IsWarning);
            }

            conf.Properties.BuildOptions.StopBuildOnTaskError = false;

            using (var builder = new Builder(conf)) {
                try {
                    builder.Build();
                } catch (BuilderException e) {
                    ex = e;
                }

                Assert.IsNotNull(ex);
                Assert.AreEqual(4, builder.TaskExecutionExceptions.Count);
            }
        }

        private class TaskThrowWarningsAndErrors : AOeTask {
            public override void Validate() {
                // nothing to validate
            }
            protected override void ExecuteInternal() {
                AddExecutionWarning(new TaskExecutionException(this, "1"));
                throw new TaskExecutionException(this, "2");
            }
            protected override void ExecuteTestModeInternal() {
                // nothing should happen
            }
        }
    }
}