#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IncrementalBuildHelperTest.cs) is part of Oetools.Builder.Test.
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
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Test.Utilities {
    
    [TestClass]
    public class IncrementalBuildHelperTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(IncrementalBuildHelperTest)));
                     
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
        public void GetTaskTargetsRemover_Test() {
            var prevBuilt = new PathList<OeFileBuilt> {
                new OeFileBuilt {
                    State = OeFileState.Modified,
                    Path = "source1",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        },
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        }
                    }
                },
                new OeFileBuilt {
                    State = OeFileState.Deleted,
                    Path = "source2",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        }
                    }
                },
                new OeFileBuilt {
                    State = OeFileState.Modified,
                    Path = "source3",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        }
                    }
                },
                new OeFileBuilt {
                    State = OeFileState.Added,
                    Path = "source4",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        },
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        },
                        new OeTargetFileCopy {
                            TargetFilePath = "target3",
                            DeletionMode = "1"
                        }
                    }
                }
            };
            var allSourceFiles = new PathList<OeFile> {
                new OeFile {
                    State = OeFileState.Unchanged,
                    Path = "source1",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        },
                        new OeTargetFileCopy {
                            TargetFilePath = "target3"
                        }
                    }
                },
                new OeFile {
                    State = OeFileState.Modified,
                    Path = "source3",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        },
                        new OeTargetFileCopy {
                            TargetFilePath = "target3"
                        }
                    }
                },
                new OeFile {
                    State = OeFileState.Modified,
                    Path = "source4",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        },
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        }
                    }
                }
            };
            
            var output = IncrementalBuildHelper.GetBuiltFilesWithOldTargetsToRemove(allSourceFiles, prevBuilt).ToList();
            
            // ensure unmodified prevBuilt list
            Assert.AreEqual("source1", prevBuilt.ElementAt(0).Path);
            Assert.AreEqual(OeFileState.Modified, prevBuilt.ElementAt(0).State);
            Assert.AreEqual("target1", prevBuilt.ElementAt(0).Targets[0].GetTargetPath());
            Assert.AreEqual(false, prevBuilt.ElementAt(0).Targets[0].IsDeletionMode());
            Assert.AreEqual("target2", prevBuilt.ElementAt(0).Targets[1].GetTargetPath());
            Assert.AreEqual(false, prevBuilt.ElementAt(0).Targets[1].IsDeletionMode());

            Assert.IsNotNull(output);
            Assert.AreEqual(2, output.Count);
            
            // for unchanged files, we also have the new targets
            Assert.AreEqual("source1", output[0].Path);
            Assert.AreEqual(OeFileState.Unchanged, output[0].State);
            Assert.AreEqual("target1", output[0].Targets[0].GetTargetPath());
            Assert.AreEqual(true, output[0].Targets[0].IsDeletionMode());
            Assert.AreEqual("target2", output[0].Targets[1].GetTargetPath());
            Assert.AreEqual(false, output[0].Targets[1].IsDeletionMode());
            Assert.AreEqual("target3", output[0].Targets[2].GetTargetPath());
            Assert.AreEqual(false, output[0].Targets[2].IsDeletionMode());
            
            // for modified files, we don't because they will be rebuild
            Assert.AreEqual("source3", output[1].Path);
            Assert.AreEqual(OeFileState.Modified, output[1].State);
            Assert.AreEqual("target1", output[1].Targets[0].GetTargetPath());
            Assert.AreEqual(true, output[1].Targets[0].IsDeletionMode());

        }

        [TestMethod]
        public void GetTaskSourceRemover_Test() {
            
            File.WriteAllText(Path.Combine(TestFolder, "source2"), "");
            
            var prevBuilt = new PathList<OeFileBuilt> {
                new OeFileBuilt {
                    State = OeFileState.Modified,
                    Path = "/random/source1",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        },
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        }
                    }
                },
                new OeFileBuilt {
                    State = OeFileState.Unchanged,
                    Path = Path.Combine(TestFolder, "source2"),
                    Targets = new List<OeTarget> {
                        new OeTargetArchiveZip {
                            TargetPackFilePath = "target1",
                            RelativeTargetFilePath = ""
                        }
                    }
                },
                new OeFileBuilt {
                    State = OeFileState.Added,
                    Path = "/random/source3",
                    Targets = new List<OeTarget> {
                        new OeTargetArchiveZip {
                            TargetPackFilePath = "target1",
                            RelativeTargetFilePath = ""
                        }
                    }
                }
            };
                        
            var output = IncrementalBuildHelper.GetBuiltFilesDeletedSincePreviousBuild(prevBuilt).ToList();
            
            // ensure unmodified prevBuilt list
            Assert.AreEqual("/random/source1", prevBuilt.ElementAt(0).Path);
            Assert.AreEqual(OeFileState.Modified, prevBuilt.ElementAt(0).State);
            Assert.AreEqual("target1", prevBuilt.ElementAt(0).Targets[0].GetTargetPath());
            Assert.AreEqual(false, prevBuilt.ElementAt(0).Targets[0].IsDeletionMode());
            Assert.AreEqual("target2", prevBuilt.ElementAt(0).Targets[1].GetTargetPath());
            Assert.AreEqual(false, prevBuilt.ElementAt(0).Targets[1].IsDeletionMode());
            
            Assert.IsNotNull(output);
            Assert.AreEqual(2, output.Count);
            
            Assert.AreEqual("/random/source1", output[0].Path);
            Assert.AreEqual(OeFileState.Deleted, output[0].State);
            Assert.AreEqual("target1", output[0].Targets[0].GetTargetPath());
            Assert.AreEqual(true, output[0].Targets[0].IsDeletionMode());
            Assert.AreEqual("target2", output[0].Targets[1].GetTargetPath());
            Assert.AreEqual(true, output[0].Targets[1].IsDeletionMode());
            
            Assert.AreEqual("/random/source3", output[1].Path);
            Assert.AreEqual(OeFileState.Deleted, output[1].State);
            Assert.AreEqual("target1", output[1].Targets[0].GetTargetPath());
            Assert.AreEqual(true, output[1].Targets[0].IsDeletionMode());
        }

        [TestMethod]
        public void GetSourceFilesToRebuildBecauseTheyHaveNewTargets_Test() {
            var allSourceFiles = new PathList<OeFile> {
                new OeFile {
                    State = OeFileState.Unchanged,
                    Path = "source1",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        }
                    }
                },
                new OeFile {
                    State = OeFileState.Unchanged,
                    Path = "source2",
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
                    Path = "source3",
                    TargetsFiles = new List<OeTargetFile> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target4"
                        }
                    }
                }
            };
            var prevBuilt = new PathList<OeFileBuilt> {
                new OeFileBuilt {
                    Path = "source1",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target1"
                        }
                    }
                },
                new OeFileBuilt {
                    Path = "source2",
                    Targets = new List<OeTarget> {
                        new OeTargetFileCopy {
                            TargetFilePath = "target2"
                        }
                    }
                }
            };
            
            var output = IncrementalBuildHelper.GetSourceFilesToRebuildBecauseTheyHaveNewTargets(allSourceFiles, prevBuilt).ToList();
            
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual("source2", output[0].Path);

        }

        [TestMethod]
        public void GetListOfFileToCompileBecauseOfTableCrcChanges_Test() {
            var env = new EnvExecution2();
            var previouslyBuiltFiles = new List<OeFileBuiltCompiled>();

            var output = IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfTableCrcChanges(env, previouslyBuiltFiles).ToList();

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
            
            output = IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfTableCrcChanges(env, previouslyBuiltFiles).ToList();

            Assert.AreEqual(2, output.Count);
            Assert.IsTrue(output.Exists(f => f.Path.Equals("source1")));
            Assert.IsTrue(output.Exists(f => f.Path.Equals("source3")));

            env.SequencesSet = new HashSet<string> {
                "sequence1"
            };
            env.TablesCrcSet = new Dictionary<string, string> {
                {
                    "table2", "crc2"
                }
            };

            output = IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfTableCrcChanges(env, previouslyBuiltFiles).ToList();

            Assert.AreEqual(0, output.Count, "we should have nothing");
            
            env.TablesCrcSet = new Dictionary<string, string> {
                {
                    "table2", "crcdifferent"
                }
            };
            output = IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfTableCrcChanges(env, previouslyBuiltFiles).ToList();

            Assert.AreEqual(1, output.Count, "we should have source 3 now because the table CRC has changed");
            Assert.IsTrue(output.Exists(f => f.Path.Equals("source3")));

        }
        
        private class EnvExecution2 : UoeExecutionEnv {
            
            public override Dictionary<string, string> TablesCrc => TablesCrcSet;
            public override HashSet<string> Sequences => SequencesSet;

            public Dictionary<string, string> TablesCrcSet { get; set; } = new Dictionary<string, string>();
            public HashSet<string> SequencesSet { get; set; } = new HashSet<string>();
        }
        
        [TestMethod]
        public void GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification_Test() {
            var modifiedFiles = new PathList<OeFile>();
            var previouslyBuiltFiles = new List<OeFileBuiltCompiled>();

            var output = IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(0, output.Count, "empty for now");

            modifiedFiles = new PathList<OeFile> {
                new OeFile("file1"),
                new OeFile("file2"),
                new OeFile("file3"),
                new OeFile("file4")
            };
            
            output = IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(modifiedFiles, previouslyBuiltFiles).ToList();

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
            
            output = IncrementalBuildHelper.GetSourceFilesToRebuildBecauseOfDependenciesModification(modifiedFiles, previouslyBuiltFiles).ToList();

            Assert.AreEqual(2, output.Count, "source2 and source3 should be included");
            Assert.AreEqual("source3", output[0].Path, "source2 should be included since it requires source3 which is now also rebuilt");
            Assert.AreEqual("source2", output[1].Path, "source3 should be included since it requires file1 which need to be rebuilt");
        }
    }
}