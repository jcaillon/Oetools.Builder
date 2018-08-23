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
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
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
        public void Builder_Source_Test() {
            var sourceDirectory = Path.Combine(TestFolder, "source1");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            var builder = new Builder(new OeBuildConfiguration {
            });
            builder.SourceDirectory = sourceDirectory;
            
        }
        
        /// <summary>
        /// Tests that we get what we need in <see cref="Builder.BuildHistory"/> for <see cref="OeBuildHistory.CompilationProblems"/>
        /// </summary>
        [TestMethod]
        public void Builder_History_Compiled_Files_Test() {
            var sourceDirectory = Path.Combine(TestFolder, "source1");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file1.p"), "quit."); // compile ok
            File.WriteAllText(Path.Combine(sourceDirectory, "file2.w"), "quit. quit."); // compile with warnings
            File.WriteAllText(Path.Combine(sourceDirectory, "file3.p"), "nof..sense, will not compile"); // compile with errors

            var builder = new Builder(new OeBuildConfiguration {
                BuildSourceTasks = new List<OeBuildStepCompile> {
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCompile { Include = "**" }
                        }
                    },
                    new OeBuildStepCompile {
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCopy { Include = "**" }
                        }
                    }
                }
            }) {
                SourceDirectory = sourceDirectory
            };

            Assert.AreEqual(true, builder.BuildConfiguration.Properties.IncrementalBuildOptions.Enabled); 
            Assert.AreEqual(false, builder.FullRebuild);

            try {
                builder.Build();
                Assert.Fail("The build should fail");
            } catch (BuilderException e) {
                Assert.AreEqual(typeof(TaskExecutorException), e.InnerException.GetType());
            }
            
            //var pb = builder.BuildHistory.CompilationProblems.ToList();
            //
            //Assert.AreEqual(5, pb.Count);
            
            builder.Dispose();            
            
        }
        
        private class OeTaskFileTargetFileCompile : OeTaskFileTargetFileCopy, IOeTaskCompile { }
        
        private class OeTaskFileTargetFileCopy : OeTaskFileTargetFile {
            protected override void ExecuteForFilesInternal(IEnumerable<IOeFileToBuildTargetFile> files) { }
            public override void Validate() { }
            
        }
        
        [TestMethod]
        public void GetDeletedFileList_Test() {
            var sourceDirectory = Path.Combine(TestFolder, "source_deleted");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "file1"), "");
            
            var deletedFiles = Builder.GetDeletedFileList(new List<OeFileBuilt> {
                new OeFileBuilt {
                    SourceFilePath = Path.Combine(sourceDirectory, "file1")
                },
                new OeFileBuilt {
                    SourceFilePath = Path.Combine(sourceDirectory, "file2")
                },
                new OeFileBuilt {
                    SourceFilePath = Path.Combine(sourceDirectory, "file3"),
                    State = OeFileState.Deleted
                }
            });
            
            Assert.AreEqual(1, deletedFiles.Count);
            Assert.AreEqual(Path.Combine(sourceDirectory, "file2"), deletedFiles[0].SourceFilePath);

        }
        
        [TestMethod]
        public void Builder_Test_Cancel() {
            var builder = new Builder(new OeBuildConfiguration {
                PreBuildTasks = new List<OeBuildStepClassic> {
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
            } catch (OperationCanceledException e) {
                ex = e;
            }
            
            Assert.IsNotNull(ex);
        }
        
        [TestMethod]
        public void GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification_Test() {
            var env = new EnvExecution2();
            var modifiedFiles = new List<OeFile>();
            var previouslyBuiltFiles = new List<OeFileBuiltCompiled>();

            var output = Builder.GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(env, modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(0, output.Count, "empty for now");

            modifiedFiles = new List<OeFile> {
                new OeFile("file1"),
                new OeFile("file2"),
                new OeFile("file3"),
                new OeFile("file4")
            };
            
            output = Builder.GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(env, modifiedFiles, previouslyBuiltFiles).ToList();

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
                        "file1"
                    }
                },
                new OeFileBuiltCompiled(new OeFile("source3")) {
                    RequiredFiles = null
                }
            };
            
            output = Builder.GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(env, modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(1, output.Count, "source2 should be included");
            Assert.AreEqual("source2", output[0].SourceFilePath, "source2 should be included since it requires file1 which has been modified");

            previouslyBuiltFiles[0].RequiredDatabaseReferences = new List<OeDatabaseReference> {
                new OeDatabaseReferenceSequence {
                    QualifiedName = "sequence1"
                }
            };
            previouslyBuiltFiles[2].RequiredDatabaseReferences = new List<OeDatabaseReference> {
                new OeDatabaseReferenceTable {
                    QualifiedName = "table2",
                    Crc = "crc2"
                }
            };
            
            output = Builder.GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(env, modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(3, output.Count, "all three source should be included");
            Assert.IsTrue(output.Exists(f => f.SourceFilePath.Equals("source1")));
            Assert.IsTrue(output.Exists(f => f.SourceFilePath.Equals("source2")));
            Assert.IsTrue(output.Exists(f => f.SourceFilePath.Equals("source3")));

            env.SequencesSet = new HashSet<string> {
                "sequence1"
            };
            env.TablesCrcSet = new Dictionary<string, string> {
                {
                    "table2", "crc2"
                }
            };
            modifiedFiles.RemoveAt(0);

            output = Builder.GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(env, modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(0, output.Count, "we should have nothing, file1 isn't modified and all table/sequences are the same as before");
            
            env.TablesCrcSet = new Dictionary<string, string> {
                {
                    "table2", "crcdifferent"
                }
            };
            output = Builder.GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(env, modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(1, output.Count, "we should have source 3 now because the table CRC has changed");
            Assert.IsTrue(output.Exists(f => f.SourceFilePath.Equals("source3")));

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