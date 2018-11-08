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
            
            var sourceDirectory = Path.Combine(TestFolder, "source_build");
            Utils.CreateDirectoryIfNeeded(Path.Combine(sourceDirectory, "subfolder"));
            
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "quit."); // compile ok
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "quit. quit."); // compile with warnings
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file4.p"), "quit.");
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file5.p"), "quit.");
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "resource.ext"), "ok");
            
            var buildConfiguration1 = new OeBuildConfiguration {
                BuildSourceStepGroup = new List<OeBuildStepCompile> {
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile { Include = "**/subfolder/**", TargetDirectory = "subfolder;copy_subfolder" },
                            new OeTaskFileTargetArchiveProlibCompile { Include = "**.w", TargetProlibFilePath = "w.pl", RelativeTargetDirectory = ";screens" }
                        }
                    },
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile { Include = "**((*)).p", TargetDirectory = "{{1}}" },
                            new OeTaskFileTargetFileCopy { Include = "**.ext", TargetFilePath = "resources/file.new" }
                        }
                    }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDirectory,
                        TreatWarningsAsErrors = true,
                        StopBuildOnCompilationError = false,
                        StopBuildOnCompilationWarning = false,
                        TestMode = true
                    },
                    IncrementalBuildOptions = new OeIncrementalBuildOptions {
                        Enabled = true,
                        StoreSourceHash = true,
                        MirrorDeletedTargetsToOutput = true,
                        MirrorDeletedSourceFileToOutput = true,
                        RebuildFilesWithNewTargets = true
                    }
                }
            };

            OeBuildHistory firstBuildHistory;
            
            using (var builder = new Builder(buildConfiguration1)) {

                builder.Build();

                Assert.AreEqual(5, builder.BuildSourceHistory.BuiltFiles.Count, "5 unique files built in total");
                Assert.AreEqual(10, builder.BuildSourceHistory.BuiltFiles.SelectMany(f => f.Targets).Count(), "10 targets in total");

                // can't check compilation since it is test mode
                Assert.AreEqual(0, builder.BuildSourceHistory.CompiledFiles.Count);

                // check each file in history
                foreach (var file in builder.BuildSourceHistory.BuiltFiles) {
                    Assert.IsFalse(string.IsNullOrEmpty(file.Hash));
                    Assert.AreEqual(OeFileState.Added, file.State);
                    Assert.IsTrue(file.Size > 0);
                }

                CheckTasksWithBuildConfiguration1Targets(builder, builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath);

                firstBuildHistory = builder.BuildSourceHistory.GetDeepCopy();
            }

            // we now rebuild this project, injecting the previous history
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = firstBuildHistory
            }) {
                builder.Build();
                
                Assert.AreEqual(0, builder.BuildStepExecutors
                    .SelectMany(te => te.Tasks)
                    .OfType<IOeTaskFileBuilder>()
                    .SelectMany(t => t.GetFilesBuilt().ToNonNullList())
                    .Count(), "we expect to have 0 files built this time, nothing has changed");
                
                Assert.AreEqual(5, builder.BuildSourceHistory.BuiltFiles.Count, "5 unique files in history in total");
                Assert.AreEqual(10, builder.BuildSourceHistory.BuiltFiles.SelectMany(f => f.Targets).Count(), "10 targets in history in total");
                
                // check each file in history
                foreach (var file in builder.BuildSourceHistory.BuiltFiles) {
                    Assert.IsFalse(string.IsNullOrEmpty(file.Hash));
                    Assert.AreEqual(OeFileState.Unchanged, file.State);
                    Assert.IsTrue(file.Size > 0);
                }
            }

            // full rebuild 
            buildConfiguration1.Properties.IncrementalBuildOptions.FullRebuild = true;
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = firstBuildHistory
            }) {
                builder.Build();
                
                // we expect to have rebuild everything here
                CheckTasksWithBuildConfiguration1Targets(builder, builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath);

                Assert.AreEqual(5, builder.BuildSourceHistory.BuiltFiles.Count, "5 unique files in history in total");
                Assert.AreEqual(10, builder.BuildSourceHistory.BuiltFiles.SelectMany(f => f.Targets).Count(), "10 targets in history in total");
                
                // check each file in history
                foreach (var file in builder.BuildSourceHistory.BuiltFiles) {
                    Assert.IsFalse(string.IsNullOrEmpty(file.Hash));
                    Assert.AreEqual(OeFileState.Unchanged, file.State);
                    Assert.IsTrue(file.Size > 0);
                }
            }
            buildConfiguration1.Properties.IncrementalBuildOptions.FullRebuild = false;
            
            
            // delete a file, and delete some targets
            File.Delete(Path.Combine(sourceDirectory, "file2.w"));
            ((OeTaskFileTargetFileCompile) buildConfiguration1.BuildSourceStepGroup[0].Tasks[0]).TargetDirectory = "subfolder";
            
            using (var builder = new Builder(buildConfiguration1) {
                BuildSourceHistory = firstBuildHistory
            }) {
                builder.Build();
                
                // should find 1 deleted file and 4 unchanged
                Assert.AreEqual(1, builder.BuildSourceHistory.BuiltFiles.Count(f => f.State == OeFileState.Deleted));
                Assert.AreEqual(4, builder.BuildSourceHistory.BuiltFiles.Count(f => f.State == OeFileState.Unchanged));
                
                // should find 4 targets deleted (2 for file2.w, 1 each for file4/5)
                Assert.AreEqual(4, builder.BuildSourceHistory.BuiltFiles.SelectMany(f => f.GetAllTargets()).Count(t => t.IsDeletionMode()));
                
                Assert.AreEqual(10, builder.BuildSourceHistory.BuiltFiles.SelectMany(f => f.Targets).Count(), "still 10 targets in history in total");
                
                // we expect to have 2 extra remove tasks
                OeTaskTargetsDeleter remover1 = (OeTaskTargetsDeleter) builder.BuildStepExecutors[1].Tasks[2];
                OeTaskTargetsDeleter remover2 = (OeTaskTargetsDeleter) builder.BuildStepExecutors[1].Tasks[3];
                
                Assert.AreEqual(Path.Combine(sourceDirectory, "file2.w"), remover1.GetFilesBuilt().ElementAt(0).Path);
                
                Assert.AreEqual(Path.Combine(sourceDirectory, "subfolder", "file4.p"), remover2.GetFilesBuilt().ElementAt(0).Path);
                Assert.AreEqual(Path.Combine(sourceDirectory, "subfolder", "file5.p"), remover2.GetFilesBuilt().ElementAt(1).Path);
                
                
            }

        }

        private void CheckTasksWithBuildConfiguration1Targets(Builder builder, string outputDirectory) {
            // check first task
            IOeTaskFileBuilder task = (OeTaskFileTargetFileCompile) builder.BuildStepExecutors[0].Tasks[0];
            var filesBuilt = task.GetFilesBuilt().ToList();
            Assert.AreEqual(2, filesBuilt.Count, "2 files built on the first task");
            Assert.AreEqual(4, filesBuilt.SelectMany(f => f.Targets).Count(), "4 targets built on the first task");
            Assert.AreEqual(Path.Combine(outputDirectory, "subfolder", "file4.r"), filesBuilt[0].GetAllTargets().ElementAt(0).GetTargetPath());
            Assert.AreEqual(Path.Combine(outputDirectory, "copy_subfolder", "file4.r"), filesBuilt[0].GetAllTargets().ElementAt(1).GetTargetPath());
            Assert.AreEqual(Path.Combine(outputDirectory, "subfolder", "file5.r"), filesBuilt[1].GetAllTargets().ElementAt(0).GetTargetPath());
            Assert.AreEqual(Path.Combine(outputDirectory, "copy_subfolder", "file5.r"), filesBuilt[1].GetAllTargets().ElementAt(1).GetTargetPath());

            // check the second task
            task = (OeTaskFileTargetArchiveProlibCompile) builder.BuildStepExecutors[0].Tasks[1];
            filesBuilt = task.GetFilesBuilt().ToList();
            Assert.AreEqual(1, filesBuilt.Count, "1 file built on the second task");
            Assert.AreEqual(2, filesBuilt.SelectMany(f => f.Targets).Count(), "2 targets built on the second task");
            Assert.AreEqual(Path.Combine(outputDirectory, "w.pl"), ((OeTargetArchiveProlib) filesBuilt[0].Targets[0]).TargetPackFilePath);
            Assert.AreEqual("file2.r", ((OeTargetArchiveProlib) filesBuilt[0].Targets[0]).RelativeTargetFilePath);
            Assert.AreEqual(Path.Combine(outputDirectory, "w.pl"), ((OeTargetArchiveProlib) filesBuilt[0].Targets[1]).TargetPackFilePath);
            Assert.AreEqual(Path.Combine("screens", "file2.r"), ((OeTargetArchiveProlib) filesBuilt[0].Targets[1]).RelativeTargetFilePath);

            // check the third task
            task = (OeTaskFileTargetFileCompile) builder.BuildStepExecutors[1].Tasks[0];
            filesBuilt = task.GetFilesBuilt().ToList();
            Assert.AreEqual(3, filesBuilt.Count, "3 files built on the third task (1 didn't compile so it was not built)");
            Assert.AreEqual(3, filesBuilt.SelectMany(f => f.Targets).Count(), "3 targets built on the third task");
            Assert.AreEqual(Path.Combine(outputDirectory, "file1", "file1.r"), ((OeTargetFileCopy) filesBuilt[0].Targets[0]).TargetFilePath);
            Assert.AreEqual(Path.Combine(outputDirectory, "file4", "file4.r"), ((OeTargetFileCopy) filesBuilt[1].Targets[0]).TargetFilePath);
            Assert.AreEqual(Path.Combine(outputDirectory, "file5", "file5.r"), ((OeTargetFileCopy) filesBuilt[2].Targets[0]).TargetFilePath);

            // check the fourth task
            task = (OeTaskFileTargetFileCopy) builder.BuildStepExecutors[1].Tasks[1];
            filesBuilt = task.GetFilesBuilt().ToList();
            Assert.AreEqual(1, filesBuilt.Count, "1 file built on the fourth task");
            Assert.AreEqual(1, filesBuilt.SelectMany(f => f.Targets).Count(), "1 target built on the fourth task");
            Assert.AreEqual(Path.Combine(outputDirectory, "resources", "file.new"), ((OeTargetFileCopy) filesBuilt[0].Targets[0]).TargetFilePath);
        }
        
        [TestMethod]
        public void Builder_Test_Build_Pre_post_tasks() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var sourceDirectory = Path.Combine(TestFolder, "source_test_pre_post");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "");
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "");
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.ext"), "");
            
            var builder = new Builder(new OeBuildConfiguration {
                PreBuildStepGroup = new List<OeBuildStepClassic> {
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCopy2 { Include = "{{SOURCE_DIRECTORY}}/**.w", TargetDirectory = "{{SOURCE_DIRECTORY}}/copied_w" },
                            new OeTaskFileTargetArchiveProlibCompileProlib2 { Include = "{{SOURCE_DIRECTORY}}/**file1**", TargetProlibFilePath = "{{SOURCE_DIRECTORY}}/my.pl", RelativeTargetDirectory = "" }
                        }
                    }
                },
                PostBuildStepGroup = new List<OeBuildStepClassic> {
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCopy2 { Include = "{{SOURCE_DIRECTORY}}/**", Exclude = "**((.p||.w))", TargetDirectory = "{{SOURCE_DIRECTORY}}/copied_ext" }
                        }
                    }  
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDirectory,
                        OutputDirectoryPath = sourceDirectory,
                        TreatWarningsAsErrors = true
                    }
                }
            });
            
            builder.Build();
            
            var filesBuilt = builder.BuildStepExecutors
                .SelectMany(exec => exec?.Tasks)
                .OfType<IOeTaskFileBuilder>()
                .SelectMany(t => t.GetFilesBuilt().ToNonNullList())
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
                BuildOutputStepGroup = new List<OeBuildStepClassic> {
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCopy2 { Include = "**.w", TargetDirectory = "copied_w" },
                            new OeTaskFileTargetArchiveProlibCompileProlib2 { Include = "**file1**", TargetProlibFilePath = "my.pl", RelativeTargetDirectory = "" }
                        }
                    },
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
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
            
            var filesBuilt = builder.BuildStepExecutors
                .SelectMany(exec => exec?.Tasks)
                .OfType<IOeTaskFileBuilder>()
                .SelectMany(t => t.GetFilesBuilt().ToNonNullList())
                .ToList();
            
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
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "quit."); // compile ok
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "quit. quit."); // compile with warnings
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.p"), "nof sense, will not compile"); // compile with errors


            var builder = new Builder(new OeBuildConfiguration {
                BuildSourceStepGroup = new List<OeBuildStepCompile> {
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**file1**", TargetDirectory = "first;/random/folder" },
                            new OeTaskFileTargetArchiveProlibCompileProlib2 { Include = "**file((2||3))**", TargetProlibFilePath = "my.pl", RelativeTargetDirectory = "" }
                        }
                    },
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**file1**", TargetDirectory = "second" },
                        }
                    }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDirectory,
                        StopBuildOnCompilationError = false,
                        StopBuildOnCompilationWarning = false
                    },
                    IncrementalBuildOptions = new OeIncrementalBuildOptions {
                        StoreSourceHash = true
                    }
                }
            }) {
                BuildSourceHistory = new OeBuildHistory {
                    BuiltFiles = new List<OeFileBuilt> {
                        new OeFileBuilt {
                            Path = "myfile.p",
                            Size = 2,
                            State = OeFileState.Modified,
                            Targets = new List<OeTarget> {
                                new OeTargetFileCopy {
                                    TargetFilePath = "derp.out.p"
                                }
                            },
                            Hash = "okay"
                        }
                    }
                }
            };
            Assert.AreEqual(true, builder.BuildConfiguration.Properties.IncrementalBuildOptions.Enabled);

            builder.Build();
            
            Assert.AreEqual(3, builder.BuildSourceHistory.BuiltFiles.Count);
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "first", "file1.r"), builder.BuildSourceHistory.BuiltFiles[0].GetAllTargets().ToList()[0].GetTargetPath());
            Assert.AreEqual(Path.Combine("C:\\random\\folder", "file1.r"), builder.BuildSourceHistory.BuiltFiles[0].GetAllTargets().ToList()[1].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "my.pl", "file2.r"), builder.BuildSourceHistory.BuiltFiles[1].GetAllTargets().ToList()[0].GetTargetPath());
            Assert.AreEqual("derp.out.p", builder.BuildSourceHistory.BuiltFiles[2].GetAllTargets().ToList()[0].GetTargetPath());
            
            // we asked for hash
            Assert.IsFalse(string.IsNullOrEmpty(builder.BuildSourceHistory.BuiltFiles[0].Hash));
            Assert.IsFalse(string.IsNullOrEmpty(builder.BuildSourceHistory.BuiltFiles[1].Hash));
            
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
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.p"), "nof sense, will not compile"); // compile with errors

            var builder = new Builder(new OeBuildConfiguration {
                BuildSourceStepGroup = new List<OeBuildStepCompile> {
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**file1**", TargetDirectory = "first;/random/folder" },
                            new OeTaskFileTargetArchiveProlibCompileProlib2 { Include = "**file((2||3))**", TargetProlibFilePath = "my.pl", RelativeTargetDirectory = "" }
                        }
                    },
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**file1**", TargetDirectory = "second" },
                            new OeTaskFileTargetFileCompile2 { Include = "**file3**", TargetDirectory = "second" },
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
            
            Assert.AreEqual(2, builder.BuildSourceHistory.BuiltFiles.Count, "only 2 files built because file3 doesn't compile");
            
            Assert.AreEqual(3, builder.BuildSourceHistory.CompiledFiles.SelectMany(f => f.CompilationProblems).Count(), $"builder.BuildSourceHistory.CompiledFiles.Count : {builder.BuildSourceHistory.CompiledFiles.Count}, ");
            Assert.AreEqual(1, builder.BuildSourceHistory.CompiledFiles.SelectMany(f => f.CompilationProblems).Count(p => p is OeCompilationWarning));

            builder.Dispose();        
            
        }

        private class OeTaskFileTargetArchiveProlibCompileProlib2 : OeTaskFileTargetArchiveProlibCompile {
            public override void ExecuteForFilesTargetArchives(IEnumerable<IOeFileToBuildTargetArchive> files) {
                _builtPaths = new PathList<OeFileBuilt>();
                foreach (var file in files.Cast<OeFile>()) {
                    var fileBuilt = GetNewFileBuilt(file);
                    fileBuilt.Targets = file.GetAllTargets().ToList();
                    _builtPaths.Add(fileBuilt);
                }
            }
        }

        private class OeTaskFileTargetFileCompile2 : OeTaskFileTargetFileCompile {
            public override void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files) {
                _builtPaths = new PathList<OeFileBuilt>();
                foreach (var file in files.Cast<OeFile>()) {
                    var fileBuilt = GetNewFileBuilt(file);
                    fileBuilt.Targets = file.GetAllTargets().ToList();
                    _builtPaths.Add(fileBuilt);
                }
            }
        }
        
        private class OeTaskFileTargetFileCopy2 : OeTaskFileTargetFileCopy {
            public override void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files) {
                _builtPaths = new PathList<OeFileBuilt>();
                foreach (var file in files.Cast<OeFile>()) {
                    var fileBuilt = GetNewFileBuilt(file);
                    fileBuilt.Targets = file.GetAllTargets().ToList();
                    _builtPaths.Add(fileBuilt);
                }
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
        /// Tests that we get what we need in <see cref="Builder.BuildStepExecutors"/>
        /// </summary>
        [TestMethod]
        public void Builder_Test_All_Task_build_steps() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var builder = new Builder(new OeBuildConfiguration {
                PreBuildStepGroup = new List<OeBuildStepClassic> {
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new OeTaskExec2()
                        }
                    }
                },
                BuildSourceStepGroup = new List<OeBuildStepCompile> {
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskExec2()
                        }
                    }
                },
                BuildOutputStepGroup = new List<OeBuildStepClassic> {
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new OeTaskExec2()
                        }
                    }
                },
                PostBuildStepGroup = new List<OeBuildStepClassic> {
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new OeTaskExec2()
                        }
                    },
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
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
                PreBuildStepGroup = new List<OeBuildStepClassic> {
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
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

        private class TaskWaitForCancel : OeTask {
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
    }
}