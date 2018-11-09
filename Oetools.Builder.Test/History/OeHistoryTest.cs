#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeHistoryTest.cs) is part of Oetools.Builder.Test.
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
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.History {
    
    [TestClass]
    public class OeHistoryTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(OeHistoryTest)));
                     
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
        public void AllSerializableClassInHistoryShouldSerialize() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.History)}")) {
                var attr = type.GetCustomAttributes(typeof(SerializableAttribute), true);
                if (attr != null && attr.Length > 0 && type.IsPublic) {
                    try {
                        new XmlSerializer(type);
                    } catch (Exception e) {
                        Assert.IsNull(e, $"FAILED TO SERIALIZE : {type.Name} : {e}");
                    }
                }
            }
        }

        [TestMethod]
        public void OeBuildHistory_Serialization_Test() {
            var history = new OeBuildHistory {
                CompiledFiles = new List<OeCompiledFile> {
                    new OeCompiledFile {
                        Path = @"C:\initialsource\compiled1",
                        CompilationProblems = new List<AOeCompilationProblem> {
                            new OeCompilationError {
                                FilePath = @"C:\initialsource\include1"
                            },
                            new OeCompilationWarning {
                                FilePath = @"C:\initialsource\include2"
                            }
                        }
                    },
                    new OeCompiledFile {
                        Path = @"C:\initialsource\compiled2",
                        CompilationProblems = new List<AOeCompilationProblem> {
                            new OeCompilationWarning {
                                FilePath = @"C:\initialsource\include2"
                            }
                        }
                    }
                },
                BuiltFiles = new List<OeFileBuilt> {
                    new OeFileBuilt {
                        Path = @"C:\initialsource\source1",
                        Targets = new List<AOeTarget> {
                            new OeTargetFile {
                                FilePath = @"D:\initialtarget\target1"
                            },
                            new OeTargetCab {
                                ArchiveFilePath = @"D:\initialtarget\targetcab1",
                                FilePath = ""
                            },
                            new OeTargetProlib {
                                ArchiveFilePath = @"D:\initialtarget\targetprolib1",
                                FilePath = ""
                            },
                            new OeTargetZip {
                                ArchiveFilePath = @"D:\initialtarget\targetzip1",
                                FilePath = ""
                            }
                        }
                    },
                     new OeFileBuiltCompiled {
                         RequiredFiles = new List<string> {
                             @"C:\initialsource\include3",
                             @"C:\initialsource\include4"
                         }
                     }
                }
            };
            
            history.Save(Path.Combine(TestFolder, "build.xml"), @"C:\initialsource", @"D:\initialtarget");

            var loadedHistory = OeBuildHistory.Load(Path.Combine(TestFolder, "build.xml"), @"E:\newsource", @"F:\newtarget");
            
            Assert.AreEqual(@"E:\newsource\compiled1", loadedHistory.CompiledFiles[0].Path);
            Assert.AreEqual(@"E:\newsource\include1", loadedHistory.CompiledFiles[0].CompilationProblems[0].FilePath);
            Assert.AreEqual(@"E:\newsource\compiled2", loadedHistory.CompiledFiles[1].Path);
            Assert.AreEqual(@"E:\newsource\include2", loadedHistory.CompiledFiles[1].CompilationProblems[0].FilePath);
            Assert.AreEqual(@"E:\newsource\source1", loadedHistory.BuiltFiles[0].Path);
            Assert.AreEqual(@"E:\newsource\include3", ((OeFileBuiltCompiled)loadedHistory.BuiltFiles[1]).RequiredFiles[0]);
            Assert.AreEqual(@"E:\newsource\include4", ((OeFileBuiltCompiled)loadedHistory.BuiltFiles[1]).RequiredFiles[1]);
            Assert.AreEqual(@"F:\newtarget\target1", loadedHistory.BuiltFiles[0].Targets[0].GetTargetPath());
            Assert.AreEqual(@"F:\newtarget\targetcab1", loadedHistory.BuiltFiles[0].Targets[1].GetTargetPath());
            Assert.AreEqual(@"F:\newtarget\targetprolib1", loadedHistory.BuiltFiles[0].Targets[2].GetTargetPath());
            Assert.AreEqual(@"F:\newtarget\targetzip1", loadedHistory.BuiltFiles[0].Targets[3].GetTargetPath());
        }
        
    }
}