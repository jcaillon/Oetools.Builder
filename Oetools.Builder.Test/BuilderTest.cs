using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.History;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Test {
    
    [TestClass]
    public class BuilderTest {
        
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
            Assert.AreEqual("source2", output[0].SourcePath, "source2 should be included since it requires file1 which has been modified");

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
            Assert.IsTrue(output.Exists(f => f.SourcePath.Equals("source1")));
            Assert.IsTrue(output.Exists(f => f.SourcePath.Equals("source2")));
            Assert.IsTrue(output.Exists(f => f.SourcePath.Equals("source3")));

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
            Assert.IsTrue(output.Exists(f => f.SourcePath.Equals("source3")));

        }

        private class EnvExecution2 : EnvExecution {
            
            public override Dictionary<string, string> TablesCrc => TablesCrcSet;
            public override HashSet<string> Sequences => SequencesSet;

            public Dictionary<string, string> TablesCrcSet { get; set; } = new Dictionary<string, string>();
            public HashSet<string> SequencesSet { get; set; } = new HashSet<string>();
        }
    }
}