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
using Oetools.Utilities.Openedge.Execution;

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
        public void Builder_Test_Realistic_builds() {
            
            var sourceDirectory = Path.Combine(TestFolder, "source_build");
            
            Utils.CreateDirectoryIfNeeded(Path.Combine(sourceDirectory, "subfolder"));
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "quit."); // compile ok
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "quit. quit."); // compile with warnings
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.p"), "nof sense, will not compile"); // compile with errors
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file4.p"), "");
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file5.p"), "");
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "resource.ext"), "");
            
            var builder = new Builder(new OeBuildConfiguration {
                BuildSourceStepGroup = new List<OeBuildStepCompile> {
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**/subfolder/**", TargetDirectory = "subfolder;copy_subfolder" },
                            new OeTaskFileTargetArchiveCompileProlib2 { Include = "**.w", TargetProlibFilePath = "w.pl", RelativeTargetDirectory = ";screens" }
                        }
                    },
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile2 { Include = "**((*)).p", TargetDirectory = "{{1}}" },
                            new OeTaskFileTargetFileCopy2 { Include = "**.ext", TargetFilePath = "resources/file.new" }
                        }
                    }
                },
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        SourceDirectoryPath = sourceDirectory,
                        TreatWarningsAsErrors = true,
                        StopBuildOnCompilationError = false,
                        StopBuildOnCompilationWarning = false
                    },
                    IncrementalBuildOptions = new OeIncrementalBuildOptions {
                        Enabled = true,
                        MirrorDeletedTargetsToOutput = true,
                        MirrorDeletedSourceFileToOutput = true,
                        StoreSourceHash = true,
                        RebuildFilesWithNewTargets = true
                    }
                }
            });
            
            //builder.Build();
            
        }

        [TestMethod]
        public void Builder_Test_Build_Pre_post_tasks() {
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
                            new OeTaskFileTargetArchiveCompileProlib2 { Include = "{{SOURCE_DIRECTORY}}/**file1**", TargetProlibFilePath = "{{SOURCE_DIRECTORY}}/my.pl", RelativeTargetDirectory = "" }
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
                .Where(t => t is IOeTaskFileBuilder)
                .Cast<IOeTaskFileBuilder>()
                .SelectMany(t => t.GetFilesBuilt().ToNonNullList())
                .ToList();
            
            Assert.AreEqual(1, ((IOeTaskCompile) builder.BuildStepExecutors[0].Tasks.ToList()[1]).GetCompiledFiles().Count);
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "copied_w", "file2.w"), filesBuilt[0].Targets[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "my.pl", "file1.r"), filesBuilt[1].Targets[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "copied_ext", "file3.ext"), filesBuilt[2].Targets[0].GetTargetPath());
        }

        [TestMethod]
        public void Builder_Test_Build_output() {
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
                            new OeTaskFileTargetArchiveCompileProlib2 { Include = "**file1**", TargetProlibFilePath = "my.pl", RelativeTargetDirectory = "" }
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
                .Where(t => t is IOeTaskFileBuilder)
                .Cast<IOeTaskFileBuilder>()
                .SelectMany(t => t.GetFilesBuilt().ToNonNullList())
                .ToList();
            
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "copied_w", "file2.w"), filesBuilt[0].Targets[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "my.pl", "file1.r"), filesBuilt[1].Targets[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "copied_ext", "file3.ext"), filesBuilt[2].Targets[0].GetTargetPath());
        }

        [TestMethod]
        public void Builder_Test_Source_History_And_Compilation_problems() {
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
                            new OeTaskFileTargetArchiveCompileProlib2 { Include = "**file((2||3))**", TargetProlibFilePath = "my.pl", RelativeTargetDirectory = "" }
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
                PreviouslyBuiltFiles = new List<OeFileBuilt> {
                    new OeFileBuilt {
                        SourceFilePath = "myfile.p",
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
            };

            Assert.AreEqual(true, builder.BuildConfiguration.Properties.IncrementalBuildOptions.Enabled); 
            Assert.AreEqual(false, builder.FullRebuild);

            builder.Build();
            
            Assert.AreEqual(3, builder.BuildHistory.CompilationProblems.Count);
            Assert.AreEqual(1, builder.BuildHistory.CompilationProblems.Count(p => p is OeCompilationWarning));

            Assert.AreEqual(3, builder.BuildHistory.BuiltFiles.Count);
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "first", "file1.r"), builder.BuildHistory.BuiltFiles[0].GetAllTargets().ToList()[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "second", "file1.r"), builder.BuildHistory.BuiltFiles[0].GetAllTargets().ToList()[1].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "my.pl", "file2.r"), builder.BuildHistory.BuiltFiles[1].GetAllTargets().ToList()[0].GetTargetPath());
            Assert.AreEqual("derp.out.p", builder.BuildHistory.BuiltFiles[2].GetAllTargets().ToList()[0].GetTargetPath());
            
            // we asked for hash
            Assert.IsFalse(string.IsNullOrEmpty(builder.BuildHistory.BuiltFiles[0].Hash));
            Assert.IsFalse(string.IsNullOrEmpty(builder.BuildHistory.BuiltFiles[1].Hash));
            
            builder.Dispose();        
            
        }

        [TestMethod]
        public void Builder_Test_Compilation_problems() {
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
                            new OeTaskFileTargetArchiveCompileProlib2 { Include = "**file((2||3))**", TargetProlibFilePath = "my.pl", RelativeTargetDirectory = "" }
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
                        SourceDirectoryPath = sourceDirectory,
                        StopBuildOnCompilationError = false,
                        StopBuildOnCompilationWarning = false
                    },
                    IncrementalBuildOptions = new OeIncrementalBuildOptions {
                        StoreSourceHash = true
                    }
                }
            }) {
                PreviouslyBuiltFiles = new List<OeFileBuilt> {
                    new OeFileBuilt {
                        SourceFilePath = "myfile.p",
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
            };

            Assert.AreEqual(true, builder.BuildConfiguration.Properties.IncrementalBuildOptions.Enabled); 
            Assert.AreEqual(false, builder.FullRebuild);

            builder.Build();
            
            Assert.AreEqual(3, builder.BuildHistory.CompilationProblems.Count);
            Assert.AreEqual(1, builder.BuildHistory.CompilationProblems.Count(p => p is OeCompilationWarning));

            Assert.AreEqual(3, builder.BuildHistory.BuiltFiles.Count);
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "first", "file1.r"), builder.BuildHistory.BuiltFiles[0].GetAllTargets().ToList()[0].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "second", "file1.r"), builder.BuildHistory.BuiltFiles[0].GetAllTargets().ToList()[1].GetTargetPath());
            Assert.AreEqual(Path.Combine(builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath, "my.pl", "file2.r"), builder.BuildHistory.BuiltFiles[1].GetAllTargets().ToList()[0].GetTargetPath());
            Assert.AreEqual("derp.out.p", builder.BuildHistory.BuiltFiles[2].GetAllTargets().ToList()[0].GetTargetPath());
            
            // we asked for hash
            Assert.IsFalse(string.IsNullOrEmpty(builder.BuildHistory.BuiltFiles[0].Hash));
            Assert.IsFalse(string.IsNullOrEmpty(builder.BuildHistory.BuiltFiles[1].Hash));
            
            builder.Dispose();        
            
        }

        private class OeTaskFileTargetArchiveCompileProlib2 : OeTaskFileTargetArchiveCompileProlib {
            private List<OeFileBuilt> _builtFiles = new List<OeFileBuilt>();
            public override void ExecuteForFilesTargetArchives(IEnumerable<IOeFileToBuildTargetArchive> files) {
                foreach (var file in files.Cast<OeFile>()) {
                    if (file.SourcePathForTaskExecution.Contains("error")) {
                        throw new TaskExecutionException(this, $"the file has error in its name : {file.SourcePathForTaskExecution}");
                    } 
                    if (file.SourcePathForTaskExecution.Contains("warning")) {
                        AddExecutionWarning(new TaskExecutionException(this, $"the file has warning in its name : {file.SourcePathForTaskExecution}"));
                    } else {
                        _builtFiles.Add(new OeFileBuilt(file) {
                            Targets = file.GetAllTargets().ToList()
                        });
                    }
                }
            }
            public override IEnumerable<OeFileBuilt> GetFilesBuilt() => _builtFiles;
        }

        private class OeTaskFileTargetFileCompile2 : OeTaskFileTargetFileCompile {
            private List<OeFileBuilt> _builtFiles = new List<OeFileBuilt>();
            public override void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files) {
                foreach (var file in files.Cast<OeFile>()) {
                    if (file.SourcePathForTaskExecution.Contains("error")) {
                        throw new TaskExecutionException(this, $"the file has error in its name : {file.SourcePathForTaskExecution}");
                    } 
                    if (file.SourcePathForTaskExecution.Contains("warning")) {
                        AddExecutionWarning(new TaskExecutionException(this, $"the file has warning in its name : {file.SourcePathForTaskExecution}"));
                    } else {
                        _builtFiles.Add(new OeFileBuilt(file) {
                            Targets = file.GetAllTargets().ToList()
                        });
                    }
                }
            }
            public override IEnumerable<OeFileBuilt> GetFilesBuilt() => _builtFiles;
        }
        
        private class OeTaskFileTargetFileCopy2 : OeTaskFileTargetFileCopy {
            private List<OeFileBuilt> _builtFiles = new List<OeFileBuilt>();
            public override void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files) {
                foreach (var file in files.Cast<OeFile>()) {
                    if (file.SourcePathForTaskExecution.Contains("error")) {
                        throw new TaskExecutionException(this, $"the file has error in its name : {file.SourcePathForTaskExecution}");
                    } 
                    if (file.SourcePathForTaskExecution.Contains("warning")) {
                        AddExecutionWarning(new TaskExecutionException(this, $"the file has warning in its name : {file.SourcePathForTaskExecution}"));
                    } else {
                        _builtFiles.Add(new OeFileBuilt(file) {
                            Targets = file.GetAllTargets().ToList()
                        });
                    }
                }
            }
            public override IEnumerable<OeFileBuilt> GetFilesBuilt() => _builtFiles;
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Builder_Constructor_set_default_values(bool useProject) {
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
        /// Tests that we get what we need in <see cref="Builder.BuildHistory"/> for <see cref="OeBuildHistory.CompilationProblems"/>
        /// </summary>
        [TestMethod]
        public void Builder_Test_All_Task_build_steps() {

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
            var builder = new Builder(new OeBuildConfiguration {
                PreBuildStepGroup = new List<OeBuildStepClassic> {
                    new OeBuildStepClassic {
                        Tasks = new List<OeTask> {
                            new TaskWaitForCancel()
                        }
                    }
                }
            });
            
            Task.Factory.StartNew(() => {
                Thread.Sleep(1000);
                builder.Cancel();
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

        [TestMethod]
        public void GetTaskTargetsRemover_Test() {
            var prevBuilt = new List<OeFileBuilt> {
                new OeFileBuilt {
                    SourceFilePath = "source1",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        }
                    }
                },
                new OeFileBuilt {
                    State = OeFileState.Deleted,
                    SourceFilePath = "source2",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        }
                    }
                },
                new OeFileBuilt {
                    SourceFilePath = "source3",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target5"
                        }
                    }
                },
            };
            var allSourceFiles = new List<OeFile> {
                new OeFile {
                    State = OeFileState.Unchanged,
                    SourceFilePath = "source1",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target3"
                        }
                    }
                },
                new OeFile {
                    State = OeFileState.Deleted,
                    SourceFilePath = "source3",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target4"
                        },
                        new OeTargetFileCopy {
                            TargetFilePath = "target5"
                        }
                    }
                }
            };
            var output = Builder.GetTaskTargetsRemover(allSourceFiles, prevBuilt, null) as OeTaskTargetsRemover;
            
            Assert.IsNotNull(output);
            Assert.AreEqual(1, output.FilesWithTargetsToRemove.Count, "source1 only should appear, source 3 has a new target but still has the old one and source 2 was deleted in previous build so those targets are already gone");
            Assert.AreEqual("source1", output.FilesWithTargetsToRemove[0].SourceFilePath);

        }

        [TestMethod]
        public void GetTaskSourceRemover_Test() {
            
            File.WriteAllText(Path.Combine(TestFolder, "source3"), "");
            
            var prevBuilt = new List<OeFileBuilt> {
                new OeFileBuilt {
                    SourceFilePath = "/random/source1",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        }
                    }
                },
                new OeFileBuilt {
                    SourceFilePath = "/random/source2",
                    Targets = new List<OeTarget> {
                        new OeTargetArchiveZip {
                            TargetPackFilePath = "target2",
                            RelativeTargetFilePath = ""
                        }
                    }
                },
                new OeFileBuilt {
                    SourceFilePath = Path.Combine(TestFolder, "source3"),
                    Targets = new List<OeTarget> {
                        new OeTargetArchiveZip {
                            TargetPackFilePath = "target3",
                            RelativeTargetFilePath = ""
                        }
                    }
                }
            };
                        
            var output = Builder.GetTaskSourceRemover(prevBuilt, null) as OeTaskTargetsRemover;
            
            Assert.IsNotNull(output);
            Assert.AreEqual(2, output.FilesWithTargetsToRemove.Count);
            Assert.AreEqual("/random/source1", output.FilesWithTargetsToRemove[0].SourceFilePath);
            Assert.AreEqual("/random/source2", output.FilesWithTargetsToRemove[1].SourceFilePath);
        }

        [TestMethod]
        public void GetSourceFilesToRebuildBecauseTheyHaveNewTargets_Test() {
            var allSourceFiles = new List<OeFile> {
                new OeFile {
                    State = OeFileState.Unchanged,
                    SourceFilePath = "source1",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        }
                    }
                },
                new OeFile {
                    State = OeFileState.Unchanged,
                    SourceFilePath = "source2",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        }
                    },
                    TargetsArchives = new List<OeTargetArchive> {
                        new OeTargetArchiveZip {
                            TargetPackFilePath = "target3",
                            RelativeTargetFilePath = ""
                        }
                    }
                },
                new OeFile {
                    State = OeFileState.Modified,
                    SourceFilePath = "source3",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target4"
                        }
                    }
                }
            };
            var prevBuilt = new List<OeFileBuilt> {
                new OeFileBuilt {
                    SourceFilePath = "source1",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        }
                    }
                },
                new OeFileBuilt {
                    SourceFilePath = "source2",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        }
                    }
                }
            };
            
            var output = Builder.GetSourceFilesToRebuildBecauseTheyHaveNewTargets(allSourceFiles, prevBuilt).ToList();
            
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual("source2", output[0].SourceFilePath);

        }

        [TestMethod]
        public void GetListOfFileToCompileBecauseOfTableCrcChanges_Test() {
            var env = new EnvExecution2();
            var previouslyBuiltFiles = new List<OeFileBuiltCompiled>();

            var output = Builder.GetSourceFilesToRebuildBecauseOfTableCrcChanges(env, previouslyBuiltFiles).ToList();

            Assert.AreEqual(0, output.Count, "empty for now");

            previouslyBuiltFiles = new List<OeFileBuiltCompiled> {
                new OeFileBuiltCompiled(new OeFile("source1")) {
                    RequiredDatabaseReferences = new List<OeDatabaseReference> {
                        new OeDatabaseReferenceSequence {
                            QualifiedName = "sequence1"
                        }
                    }
                },
                new OeFileBuiltCompiled(new OeFile("source2")),
                new OeFileBuiltCompiled(new OeFile("source3")) {
                    RequiredDatabaseReferences = new List<OeDatabaseReference> {
                        new OeDatabaseReferenceTable {
                            QualifiedName = "table2",
                            Crc = "crc2"
                        }
                    }
                }
            };
            
            output = Builder.GetSourceFilesToRebuildBecauseOfTableCrcChanges(env, previouslyBuiltFiles).ToList();

            Assert.AreEqual(2, output.Count);
            Assert.IsTrue(output.Exists(f => f.SourceFilePath.Equals("source1")));
            Assert.IsTrue(output.Exists(f => f.SourceFilePath.Equals("source3")));

            env.SequencesSet = new HashSet<string> {
                "sequence1"
            };
            env.TablesCrcSet = new Dictionary<string, string> {
                {
                    "table2", "crc2"
                }
            };

            output = Builder.GetSourceFilesToRebuildBecauseOfTableCrcChanges(env, previouslyBuiltFiles).ToList();

            Assert.AreEqual(0, output.Count, "we should have nothing");
            
            env.TablesCrcSet = new Dictionary<string, string> {
                {
                    "table2", "crcdifferent"
                }
            };
            output = Builder.GetSourceFilesToRebuildBecauseOfTableCrcChanges(env, previouslyBuiltFiles).ToList();

            Assert.AreEqual(1, output.Count, "we should have source 3 now because the table CRC has changed");
            Assert.IsTrue(output.Exists(f => f.SourceFilePath.Equals("source3")));

        }
        
        [TestMethod]
        public void GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification_Test() {
            var modifiedFiles = new List<OeFile>();
            var previouslyBuiltFiles = new List<OeFileBuiltCompiled>();

            var output = Builder.GetSourceFilesToRebuildBecauseOfDependencesModification(modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(0, output.Count, "empty for now");

            modifiedFiles = new List<OeFile> {
                new OeFile("file1"),
                new OeFile("file2"),
                new OeFile("file3"),
                new OeFile("file4")
            };
            
            output = Builder.GetSourceFilesToRebuildBecauseOfDependencesModification(modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(0, output.Count, "still empty");
            
            previouslyBuiltFiles = new List<OeFileBuiltCompiled> {
                new OeFileBuiltCompiled(new OeFile("source1")) {
                    RequiredFiles = new List<string> {
                        "file5",
                        "file6"
                    }
                },
                new OeFileBuiltCompiled(new OeFile("source2")) {
                    RequiredFiles = new List<string> {
                        "source3"
                    }
                },
                new OeFileBuiltCompiled(new OeFile("source3")) {
                    RequiredFiles = new List<string> {
                        "file1"
                    }
                }
            };
            
            output = Builder.GetSourceFilesToRebuildBecauseOfDependencesModification(modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(2, output.Count, "source2 and source3 should be included");
            Assert.AreEqual("source3", output[0].SourceFilePath, "source2 should be included since it requires source3 which is now also rebuilt");
            Assert.AreEqual("source2", output[1].SourceFilePath, "source3 should be included since it requires file1 which need to be rebuilt");
        }

        private class TaskWaitForCancel : OeTask {
            protected override void ExecuteInternal() {
                Log?.Debug("");
                CancelSource.Token.WaitHandle.WaitOne();
                CancelSource.Token.ThrowIfCancellationRequested();
            }
        }
        
        private class EnvExecution2 : UoeExecutionEnv {
            
            public override Dictionary<string, string> TablesCrc => TablesCrcSet;
            public override HashSet<string> Sequences => SequencesSet;

            public Dictionary<string, string> TablesCrcSet { get; set; } = new Dictionary<string, string>();
            public HashSet<string> SequencesSet { get; set; } = new HashSet<string>();
        }
    }
}