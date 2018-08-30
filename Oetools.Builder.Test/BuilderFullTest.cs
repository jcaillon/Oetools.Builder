#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (BuilderFullTest.cs) is part of Oetools.Builder.Test.
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
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Test {
    
    [TestClass]
    public class BuilderFullTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(BuilderFullTest)));
                     
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
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file4.p"), "quit.");
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "file5.p"), "quit.");
            File.WriteAllText(Path.Combine(sourceDirectory, "subfolder", "resource.ext"), "ok");
            
            var buildConfiguration = new OeBuildConfiguration {
                BuildSourceStepGroup = new List<OeBuildStepCompile> {
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile { Include = "**/subfolder/**", TargetDirectory = "subfolder;copy_subfolder" },
                            new OeTaskFileTargetArchiveCompileProlib { Include = "**.w", TargetProlibFilePath = "w.pl", RelativeTargetDirectory = ";screens" }
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
            
            using (var builder = new Builder(buildConfiguration)) {

                var outputDirectory = builder.BuildConfiguration.Properties.BuildOptions.OutputDirectoryPath;

                builder.Build();

                Assert.AreEqual(5, builder.BuildSourceHistory.BuiltFiles.Count, "5 unique files built in total, they should appear in the history (file1.p doesn't compile so it doesn't appear in the list");
                Assert.AreEqual(10, builder.BuildSourceHistory.BuiltFiles.SelectMany(f => f.Targets).Count(), "10 targets in total");

                // can't check compilation since it is test mode
                Assert.AreEqual(0, builder.BuildSourceHistory.CompiledFiles.Count);

                // check each file built
                foreach (var file in builder.BuildSourceHistory.BuiltFiles) {
                    Assert.IsFalse(string.IsNullOrEmpty(file.Hash));
                    Assert.AreEqual(OeFileState.Added, file.State);
                    Assert.IsTrue(file.Size > 0);
                }

                // check first task
                IOeTaskFileBuilder task = (OeTaskFileTargetFileCompile) builder.BuildStepExecutors[0].Tasks[0];
                var filesBuilt = task.GetFilesBuilt().ToList();
                Assert.AreEqual(2, filesBuilt.Count, "2 files built on the first task");
                Assert.AreEqual(4, filesBuilt.SelectMany(f => f.Targets).Count(), "4 targets built on the first task");
                Assert.AreEqual(Path.Combine(outputDirectory, "subfolder", "file4.r"), ((OeTargetFileCopy) filesBuilt[0].Targets[0]).TargetFilePath);
                Assert.AreEqual(Path.Combine(outputDirectory, "copy_subfolder", "file4.r"), ((OeTargetFileCopy) filesBuilt[0].Targets[1]).TargetFilePath);
                Assert.AreEqual(Path.Combine(outputDirectory, "subfolder", "file5.r"), ((OeTargetFileCopy) filesBuilt[1].Targets[0]).TargetFilePath);
                Assert.AreEqual(Path.Combine(outputDirectory, "copy_subfolder", "file5.r"), ((OeTargetFileCopy) filesBuilt[1].Targets[1]).TargetFilePath);

                // check the second task
                task = (OeTaskFileTargetArchiveCompileProlib) builder.BuildStepExecutors[0].Tasks[1];
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

                firstBuildHistory = builder.BuildSourceHistory.GetDeepCopy();
            }

            // we now rebuild this project, injecting the previous history
            using (var builder = new Builder(buildConfiguration) {
                BuildSourceHistory = firstBuildHistory
            }) {
                builder.Build();
                
                Assert.AreEqual(0, builder.BuildStepExecutors
                    .SelectMany(te => te.Tasks)
                    .Where(t => t is IOeTaskFileBuilder)
                    .Cast<IOeTaskFileBuilder>()
                    .SelectMany(t => t.GetFilesBuilt().ToNonNullList())
                    .Count(), "we expect to have 0 files built this time, nothing has changed");
            }
        }
    }
}