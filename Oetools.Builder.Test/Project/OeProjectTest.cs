#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeProjectTest.cs) is part of Oetools.Builder.Test.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.Project {
    [TestClass]
    public class OeProjectTest {
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(OeProjectTest)));

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
        public void GetAllBuildConfigurations() {
            var project = new OeProject {
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        Name = "first",
                        BuildConfigurations = new List<OeBuildConfiguration> {
                            new OeBuildConfiguration {
                                Name = "second"
                            },
                            new OeBuildConfiguration {
                                Name = "third",
                                BuildConfigurations = new List<OeBuildConfiguration> {
                                    new OeBuildConfiguration {
                                        Name = "fourth",
                                    }
                                }
                            }
                        }
                    },
                    new OeBuildConfiguration {
                        Name = "fifth"
                    }
                }
            };

            var cb = project.GetAllBuildConfigurations();

            Assert.AreEqual(5, cb.Count);
        }

        [TestMethod]
        public void GetBuildConfigurationCopy_static() {
            var project1 = new OeProject {
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        Name = "first",
                        Properties = new OeProperties {
                            DlcDirectoryPath = "globaldlc",
                            BuildOptions = new OeBuildOptions {
                                OutputDirectoryPath = "globaloutput",
                                SourceToBuildFilter = new OeSourceFilterOptions {
                                    Include = "include"
                                },
                                IncrementalBuildOptions = new OeIncrementalBuildOptions {
                                    EnabledIncrementalBuild = true
                                }
                            },
                            PropathSourceDirectoriesFilter = new OePropathFilterOptions() {
                                Include = "inc"
                            }
                        },
                        Variables = new List<OeVariable> {
                            new OeVariable {
                                Name = "globalname1"
                            }
                        },
                        BuildSteps = new List<AOeBuildStep> {
                            new OeBuildStepBuildSource {
                                Tasks = new List<AOeTask> {
                                    new OeTaskFileCopy()
                                }
                            }    
                        }
                    },
                    new OeBuildConfiguration {
                        Name = "fifth"
                    }
                }
            };
            var project2 = new OeProject {
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        Name = "second"
                    },
                    new OeBuildConfiguration {
                        Name = "third",
                        Variables = new List<OeVariable> {
                            new OeVariable {
                                Name = "localname1"
                            }
                        },
                        Properties = new OeProperties {
                            DlcDirectoryPath = "localdlc",
                            PropathSourceDirectoriesFilter = new OePropathFilterOptions {
                                Exclude = "exclude"
                            },
                            BuildOptions = new OeBuildOptions {
                                SourceToBuildFilter = new OeSourceFilterOptions {
                                    Exclude = "exclude"
                                }
                            }
                        },
                        BuildConfigurations = new List<OeBuildConfiguration> {
                            new OeBuildConfiguration {
                                Name = "fourth",
                                BuildSteps = new List<AOeBuildStep> {
                                    new OeBuildStepBuildSource {
                                        Tasks = new List<AOeTask> {
                                            new OeTaskFileCopy()
                                        }
                                    }    
                                }
                            }
                        }
                    }
                }
            };
            var project3 = new OeProject {
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        Name = "sixth",
                        BuildSteps = new List<AOeBuildStep> {
                            new OeBuildStepBuildSource {
                                Tasks = new List<AOeTask> {
                                    new OeTaskFileCopy()
                                }
                            }    
                        }
                    }
                }
            };

            var project1Path = Path.Combine(TestFolder, "project1");
            var project2Path = Path.Combine(TestFolder, "project2");
            var project3Path = Path.Combine(TestFolder, "project3");
            project1.Save(project1Path);
            project2.Save(project2Path);
            project3.Save(project3Path);

            var queue = new Queue<Tuple<string, string>>();
            queue.Enqueue(new Tuple<string, string>(project1Path, "first"));
            queue.Enqueue(new Tuple<string, string>(project2Path, "fourth"));
            queue.Enqueue(new Tuple<string, string>(project3Path, "sixth"));

            OeBuildConfiguration conf = OeProject.GetBuildConfigurationCopy(queue);
            
            Assert.AreEqual(2, conf.Variables.Count);
            Assert.AreEqual("sixth", conf.Name);
            Assert.AreEqual("localdlc", conf.Properties.DlcDirectoryPath);
            Assert.AreEqual("globaloutput", conf.Properties.BuildOptions.OutputDirectoryPath);
            Assert.AreEqual("exclude", conf.Properties.PropathSourceDirectoriesFilter.Exclude);
            Assert.AreEqual("exclude", conf.Properties.BuildOptions.SourceToBuildFilter.Exclude);
            Assert.AreEqual("include", conf.Properties.BuildOptions.SourceToBuildFilter.Include);
            Assert.AreEqual("inc", conf.Properties.PropathSourceDirectoriesFilter.Include);
            Assert.AreEqual(true, conf.Properties.BuildOptions.IncrementalBuildOptions.EnabledIncrementalBuild);
        
            Assert.AreEqual(3, conf.BuildSteps.Count);

            Assert.AreEqual("globaldlc", project1.BuildConfigurations[0].Properties.DlcDirectoryPath, "This should not modified the global properties.");
            Assert.AreEqual(null, project1.BuildConfigurations[1].Properties?.BuildOptions?.IncrementalBuildOptions, "This should also not modified the properties of the original build configurations.");
            
        }

        [TestMethod]
        public void GetBuildConfigurationCopy_and_inheritance() {
            var project = new OeProject {
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        Name = "first",
                        Properties = new OeProperties {
                            DlcDirectoryPath = "globaldlc",
                            BuildOptions = new OeBuildOptions {
                                OutputDirectoryPath = "globaloutput",
                                SourceToBuildFilter = new OeSourceFilterOptions {
                                    Include = "include"
                                },
                                IncrementalBuildOptions = new OeIncrementalBuildOptions {
                                    EnabledIncrementalBuild = true
                                }
                            },
                            PropathSourceDirectoriesFilter = new OePropathFilterOptions() {
                                Include = "inc"
                            }
                        },
                        Variables = new List<OeVariable> {
                            new OeVariable {
                                Name = "globalname1"
                            }
                        },
                        BuildSteps = new List<AOeBuildStep> {
                            new OeBuildStepBuildSource {
                                Tasks = new List<AOeTask> {
                                    new OeTaskFileCopy()
                                }
                            }    
                        },
                        BuildConfigurations = new List<OeBuildConfiguration> {
                            new OeBuildConfiguration {
                                Name = "second"
                            },
                            new OeBuildConfiguration {
                                Name = "third",
                                Variables = new List<OeVariable> {
                                    new OeVariable {
                                        Name = "localname1"
                                    }
                                },
                                Properties = new OeProperties {
                                    DlcDirectoryPath = "localdlc",
                                    PropathSourceDirectoriesFilter = new OePropathFilterOptions {
                                        Exclude = "exclude"
                                    },
                                    BuildOptions = new OeBuildOptions {
                                        SourceToBuildFilter = new OeSourceFilterOptions {
                                            Exclude = "exclude"
                                        }
                                    }
                                },
                                BuildConfigurations = new List<OeBuildConfiguration> {
                                    new OeBuildConfiguration {
                                        Name = "fourth",
                                        BuildSteps = new List<AOeBuildStep> {
                                            new OeBuildStepBuildSource {
                                                Tasks = new List<AOeTask> {
                                                    new OeTaskFileDelete()
                                                }
                                            }    
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new OeBuildConfiguration {
                        Name = "fifth"
                    }
                }
            };

            project.InitIds();

            for (int i = 0; i < 2; i++) {
                OeBuildConfiguration conf = project.GetBuildConfigurationCopy(i == 0 ? "fourth" : "4");

                Assert.IsNotNull(conf, i == 0 ? "Can find a configuration from name." : "Can find a configuration from id.");
                
                Assert.AreEqual(2, conf.Variables.Count);
                Assert.AreEqual("fourth", conf.Name);
                Assert.AreEqual("localdlc", conf.Properties.DlcDirectoryPath);
                Assert.AreEqual("globaloutput", conf.Properties.BuildOptions.OutputDirectoryPath);
                Assert.AreEqual("exclude", conf.Properties.PropathSourceDirectoriesFilter.Exclude);
                Assert.AreEqual("exclude", conf.Properties.BuildOptions.SourceToBuildFilter.Exclude);
                Assert.AreEqual("include", conf.Properties.BuildOptions.SourceToBuildFilter.Include);
                Assert.AreEqual("inc", conf.Properties.PropathSourceDirectoriesFilter.Include);
                Assert.AreEqual(true, conf.Properties.BuildOptions.IncrementalBuildOptions.EnabledIncrementalBuild);
            
                Assert.AreEqual(2, conf.BuildSteps.Count);

                Assert.AreEqual("globaldlc", project.BuildConfigurations[0].Properties.DlcDirectoryPath, "This should not modified the global properties.");
                Assert.AreEqual(null, project.BuildConfigurations[1].Properties?.BuildOptions?.IncrementalBuildOptions, "This should also not modified the properties of the original build configurations.");
            }
        }

        [TestMethod]
        public void StandardProject() {
            var project = OeProject.GetStandardProject();

            project.Save(Path.Combine(TestFolder, "project_default.xml"));

            // should export .xsd
            Assert.IsTrue(File.Exists(Path.Combine(TestFolder, "Project.xsd")));

            var loadedProject = OeProject.Load(Path.Combine(TestFolder, "project_default.xml"));

            // should load null values
            Assert.AreEqual(null, ((OeTaskFileCompile) loadedProject.BuildConfigurations[0].BuildSteps[0].Tasks[0]).Exclude);
        }

        [TestMethod]
        public void Serialization_And_InitIds() {
            var project = new OeProject {
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        Properties = new OeProperties {
                            AddAllSourceDirectoriesToPropath = true,
                            AddDefaultOpenedgePropath = true,
                            BuildOptions = new OeBuildOptions {
                                OutputDirectoryPath = "D:\\output",
                                ReportHtmlFilePath = Path.Combine("{{PROJECT_DIRECTORY}}", "build", "latest.html"),
                                SourceToBuildFilter = new OeSourceFilterOptions {
                                    Exclude = "**/derp", 
                                    ExcludeRegex = "\\\\[D][d]"
                                },
                                SourceToBuildGitFilter = new OeGitFilterOptions {
                                    CurrentBranchName = null, CurrentBranchOriginCommit = null, IncludeSourceFilesCommittedOnlyOnCurrentBranch = true, IncludeSourceFilesModifiedSinceLastCommit = true
                                },
                                IncrementalBuildOptions = new OeIncrementalBuildOptions {
                                    EnabledIncrementalBuild = false, MirrorDeletedSourceFileToOutput = true, UseCheckSumComparison = false, UseSimplerAnalysisForDatabaseReference = true,
                                    BuildHistoryInputFilePath = Path.Combine("{{PROJECT_DIRECTORY}}", "build", "latest.xml"),
                                    BuildHistoryOutputFilePath = Path.Combine("{{PROJECT_DIRECTORY}}", "build", "latest.xml"),
                                }
                            },
                            CompilationOptions = new OeCompilationOptions {
                                CompilableFileExtensionPattern = OeBuilderConstants.CompilableExtensionsPattern,
                                ForceSingleProcess = false,
                                TryToOptimizeCompilationDirectory = false,
                                MinimumNumberOfFilesPerProcess = 10,
                                NumberProcessPerCore = 1,
                                CompileOptions = "require-full-names, require-field-qualifiers, require-full-keywords",
                                CompileStatementExtraOptions = "MIN-SIZE = TRUE",
                                CompileWithDebugList = true,
                                CompileWithListing = true,
                                CompileWithPreprocess = true,
                                CompileWithXref = true,
                                UseCompilerMultiCompile = true
                            },
                            UseCharacterModeExecutable = false,
                            DatabaseAliases = new List<OeDatabaseAlias> {
                                new OeDatabaseAlias {
                                    AliasLogicalName = "myalias", DatabaseLogicalName = "db"
                                },
                                new OeDatabaseAlias {
                                    AliasLogicalName = "alias2", DatabaseLogicalName = "db"
                                }
                            },
                            ExtraDatabaseConnectionString = "-extra \"quotes\" ",
                            DlcDirectoryPath = "/dlc/",
                            IniFilePath = "C:\\my.ini",
                            ProcedureToExecuteAfterAnyProgressExecutionFilePath = "",
                            ProcedureToExecuteBeforeAnyProgressExecutionFilePath = "",
                            ExtraOpenedgeCommandLineParameters = "my extra param \"in quotes\" ''",
                            ProjectDatabases = new List<OeProjectDatabase> {
                                new OeProjectDatabase {
                                    DataDefinitionFilePath = "C:\\folder\\file.df", LogicalName = "db"
                                }
                            },
                            PropathEntries = new List<OePropathEntry> {
                                new OePropathEntry {Path = "entry1"}, 
                                new OePropathEntry {Path = "fezef/zef/zefzef"}, 
                                new OePropathEntry {Path = "C:\\zefzefzef\\"}
                            },
                            PropathSourceDirectoriesFilter = new OePropathFilterOptions() {
                                Exclude = "**/derp", ExcludeRegex = "\\\\[D][d]"
                            },
                            OpenedgeTemporaryDirectoryPath = "{{TEMP}}"
                        },
                        Variables = new List<OeVariable> {
                            new OeVariable {
                                Name = "MyCustomVariable", Value = "the value"
                            },
                            new OeVariable {
                                Name = "second case insensitive", Value = "new {{MyCustomVariable}}"
                            }
                        },
                        BuildConfigurations = new List<OeBuildConfiguration> {
                            new OeBuildConfiguration {
                                Name = "first conf",
                                Variables = new List<OeVariable> {
                                    new OeVariable {
                                        Name = "buildconfvar1", Value = "val"
                                    }
                                },
                                Properties = null,
                                BuildSteps = new List<AOeBuildStep> {
                                    new OeBuildStepBuildSource {
                                        Name = "step1",
                                        Tasks = new List<AOeTask> {
                                            new OeTaskFileCompile {
                                                Exclude = "**", Include = "{{**}}", TargetDirectory = "mydir"
                                            },
                                            new OeTaskFileArchiverArchiveProlib {
                                                ExcludeRegex = "regex", IncludeRegex = "regex", TargetArchivePath = "myprolib.pl", TargetDirectory = "insdide/directory",
                                            },
                                            new OeTaskFileArchiverArchiveProlibCompile {
                                                ExcludeRegex = "regex", IncludeRegex = "regex", TargetArchivePath = "path.zip", TargetDirectory = "insdide/directory"
                                            },
                                            new OeTaskFileArchiverArchiveCab {
                                                ExcludeRegex = "regex", IncludeRegex = "regex", TargetArchivePath = "myprolib.pl", TargetDirectory = "inside/directory"
                                            },
                                            new OeTaskFileArchiverArchiveCabCompile {
                                                ExcludeRegex = "regex",
                                                IncludeRegex = "regex",
                                                TargetArchivePath = "path.cab",
                                                TargetFilePath = "inside/file.p",
                                                CompressionLevel = "Max"
                                            },
                                            //new OeTaskFileTargetArchiveFtpCompile(),
                                            //new OeTaskFileTargetFileCopy(),
                                            //new OeTaskFileTargetArchiveProlib(),
                                            //new OeTaskFileTargetArchiveZip(),
                                            new OeTaskFileArchiverArchiveCab(),
                                            //new OeTaskFileTargetArchiveFtp()
                                        }
                                    },
                                    new OeBuildStepBuildSource {
                                        Name = "step2", Tasks = null
                                    },
                                    new OeBuildStepFree {
                                        Name = "step output 1",
                                        Tasks = new List<AOeTask> {
                                            new OeTaskExec {
                                                Name = "exec1",
                                                ExecutableFilePath = "exec",
                                                Parameters = "params \"quotes\"",
                                                WorkingDirectory = "dir",
                                                HiddenExecution = false,
                                                IgnoreExitCode = null
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            project.Save(Path.Combine(TestFolder, "project.xml"));

            var loadedProject = OeProject.Load(Path.Combine(TestFolder, "project.xml"));

            Assert.AreEqual(1, loadedProject.BuildConfigurations[0].BuildConfigurations[0].BuildSteps[0].Tasks[1].Id);
        }

        [TestMethod]
        public void EnsureBackWardCompatibilty() {
            string xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <BuildConfigurations>
    <Configuration>
      <Properties>
        <DlcDirectoryPath></DlcDirectoryPath>
        <ProcedureToExecuteBeforeAnyProgressExecutionFilePath />
      </Properties>
    </Configuration>
  </BuildConfigurations>
</Project>
";
            File.WriteAllText(Path.Combine(TestFolder, "input_test.xml"), xmlContent);

            var loadedProject = OeProject.Load(Path.Combine(TestFolder, "input_test.xml"));

            Assert.AreEqual(@"", loadedProject.BuildConfigurations[0].Properties.DlcDirectoryPath);
            Assert.AreEqual(null, loadedProject.BuildConfigurations[0].Properties.IniFilePath);
            Assert.AreEqual(@"", loadedProject.BuildConfigurations[0].Properties.ProcedureToExecuteBeforeAnyProgressExecutionFilePath);
        }
    }
}