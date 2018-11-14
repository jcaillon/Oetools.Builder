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
using System.Linq;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Archive;
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

        private const string EnumMethodPrefix = "GetEnum";
        
        [TestMethod]
        public void CheckForGetEnumMethodsAssociatedWithProperties() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var methodInfo in type.GetMethods().ToList().Where(m => m.Name.StartsWith(EnumMethodPrefix))) {
                    var prop = type.GetProperty(methodInfo.Name.Replace(EnumMethodPrefix, ""));
                    Assert.IsNotNull(prop, $"If {methodInfo.Name} exists, we should find the property {methodInfo.Name.Replace(EnumMethodPrefix, "")} which is not the case...");
                    Assert.IsTrue(TestHelper.CanBeNull(methodInfo.ReturnType), $"Return type of {type.Name}.{methodInfo.Name} is not nullable!");
                    Assert.IsTrue(methodInfo.ReturnType.GenericTypeArguments[0].IsEnum, $"Return type of {type.Name}.{methodInfo.Name} is not an enum!");
                }
            }
        }
        

        private const string DefaultMethodPrefix = "GetDefault";
        
        [TestMethod]
        public void CheckForGetDefaultMethodsAssociatedWithProperties() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var methodInfo in type.GetMethods().ToList().Where(m => m.IsStatic && m.Name.StartsWith(DefaultMethodPrefix))) {                   
                    var prop = type.GetProperty(methodInfo.Name.Replace(DefaultMethodPrefix, ""));
                    Assert.IsNotNull(prop, $"if {methodInfo.Name} exists, we should find the property {methodInfo.Name.Replace(DefaultMethodPrefix, "")} which is not the case...");
                    //Assert.AreEqual(methodInfo.ReturnType, prop.PropertyType, $"The method {methodInfo.Name} should return the same type as {methodInfo.Name.Replace(DefaultMethodPrefix, "")}");
                    Assert.IsTrue(prop.PropertyType.IsAssignableFrom(methodInfo.ReturnType), $"The method {methodInfo.Name} should return the same type as {methodInfo.Name.Replace(DefaultMethodPrefix, "")}");
                }
            }
        }
        
        /// <summary>
        /// We need every single public property in the <see cref="Oetools.Builder.Project"/> namespace to be nullable
        /// </summary>
        [TestMethod]
        public void CheckForNullablePublicPropertiesOnly() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var propertyInfo in type.GetProperties().ToList().Where(p => p.PropertyType.IsPublic)) {
                    Assert.IsTrue(TestHelper.CanBeNull(propertyInfo.PropertyType), $"{type.Name}.{propertyInfo.Name} is not nullable!");
                }
            }
        }
        
        /// <summary>
        /// We need every single public property in the <see cref="Oetools.Builder.Project"/> namespace to be nullable
        /// </summary>
        [TestMethod]
        public void CheckForXmlPropertiesOnEachPublicProperties() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var propertyInfo in type.GetProperties().ToList().Where(p => p.PropertyType.IsPublic && !(p.GetSetMethod()?.IsAbstract ?? false))) {
                    Assert.IsTrue(
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAnyAttributeAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlArrayAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlElementAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlIgnoreAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute), true) != null, 
                        $"{type.Name}.{propertyInfo.Name} does not have an xml attribute!");
                }
            }
        }
        
        [TestMethod]
        public void AllSerializableClassInProjectShouldSerialize() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
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
        public void OeProject_InheritGlobalPropertiesAndVariables_Test() {
            var project = new OeProject {
                GlobalVariables = new List<OeVariable> {
                    new OeVariable {
                        Name = "globalname1"
                    }
                },
                GlobalProperties = new OeProperties {
                    DlcDirectoryPath = "globaldlc",
                    BuildOptions = new OeBuildOptions {
                        OutputDirectoryPath = "globaloutput"
                    },
                    PropathSourceDirectoriesFilter = new OeFilterOptions {
                        Include = "inc"
                    }
                },
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        ConfigurationName = "first"
                    },
                    new OeBuildConfiguration {
                        ConfigurationName = "second",
                        Variables = new List<OeVariable> {
                            new OeVariable {
                                Name = "localname1"
                            }
                        },
                        Properties = new OeProperties {
                            DlcDirectoryPath = "localdlc",
                            PropathSourceDirectoriesFilter = new OeFilterOptions {
                                Exclude = "exclude"
                            }
                        }
                    }
                }
            };

            var conf = project.GetBuildConfigurationCopy("second");
            
            Assert.AreEqual(2, conf.Variables.Count);
            Assert.AreEqual("second", conf.ConfigurationName);
            Assert.AreEqual("localdlc", conf.Properties.DlcDirectoryPath);
            Assert.AreEqual("globaloutput", conf.Properties.BuildOptions.OutputDirectoryPath);
            Assert.AreEqual(null, conf.Properties.PropathSourceDirectoriesFilter.Include);
            Assert.AreEqual("exclude", conf.Properties.PropathSourceDirectoriesFilter.Exclude);
            
            Assert.AreEqual("globaldlc", project.GlobalProperties.DlcDirectoryPath, "This should not modified the global properties.");
            Assert.AreEqual(null, project.BuildConfigurations[1].Properties.BuildOptions, "This should also not modified the properties of the original build configurations.");
            

        }

        [TestMethod]
        public void Serialization_Test() {
            Utils.CreateDirectoryIfNeeded(TestFolder);
            
            var project = OeProject.GetStandardProject();
            
            project.Save(Path.Combine(TestFolder, "project_default.xml"));
            
            // should export .xsd
            Assert.IsTrue(File.Exists(Path.Combine(TestFolder, "Project.xsd")));

            var loadedProject = OeProject.Load(Path.Combine(TestFolder, "project_default.xml"));
            
            // should load null values
            Assert.AreEqual(null, loadedProject.GlobalProperties.UseCharacterModeExecutable);
            
            project.GlobalProperties = new OeProperties {
                AddAllSourceDirectoriesToPropath = true,
                AddDefaultOpenedgePropath = true,
                BuildOptions = new OeBuildOptions {
                    BuildHistoryInputFilePath = Path.Combine("{{PROJECT_DIRECTORY}}", "build", "latest.xml"),
                    BuildHistoryOutputFilePath = Path.Combine("{{PROJECT_DIRECTORY}}", "build", "latest.xml"),
                    OutputDirectoryPath = "D:\\output",
                    ReportHtmlFilePath = Path.Combine("{{PROJECT_DIRECTORY}}", "build", "latest.html")
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
                    UseCompilerMultiCompile = true,
                    UseSimplerAnalysisForDatabaseReference = true
                },
                UseCharacterModeExecutable = false,
                DatabaseAliases = new List<OeDatabaseAlias> {
                    new OeDatabaseAlias {
                        AliasLogicalName = "myalias",
                        DatabaseLogicalName = "db"
                    },
                    new OeDatabaseAlias {
                        AliasLogicalName = "alias2",
                        DatabaseLogicalName = "db"
                    }
                },
                DatabaseConnectionExtraParameters = "-extra \"quotes\" ",
                DlcDirectoryPath = "/dlc/",
                IncrementalBuildOptions = new OeIncrementalBuildOptions {
                    Enabled = false,
                    MirrorDeletedSourceFileToOutput = true,
                    StoreSourceHash = false
                },
                IniFilePath = "C:\\my.ini",
                ProcedurePathToExecuteAfterAnyProgressExecution = "",
                ProcedurePathToExecuteBeforeAnyProgressExecution = "",
                OpenedgeCommandLineExtraParameters = "my extra param \"in quotes\" ''",
                ProjectDatabases = new List<OeProjectDatabase> {
                    new OeProjectDatabase {
                        DataDefinitionFilePath = "C:\\folder\\file.df",
                        LogicalName = "db"
                    }
                },
                PropathEntries = new List<string> {
                    "entry1",
                    "fezef/zef/zefzef",
                    "C:\\zefzefzef\\"
                },
                PropathSourceDirectoriesFilter = new OeFilterOptions {
                    Exclude = "**/derp",
                    ExcludeRegex = "\\\\[D][d]"
                },
                SourceToBuildFilter =new OeFilterOptions {
                    Exclude = "**/derp",
                    ExcludeRegex = "\\\\[D][d]"
                },
                SourceToBuildGitFilterOptions = new OeGitFilterOptions {
                    CurrentBranchName = null,
                    CurrentBranchOriginCommit = null,
                    OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch = true,
                    OnlyIncludeSourceFilesModifiedSinceLastCommit = true
                },
                OpenedgeTemporaryDirectoryPath = "{{TEMP}}"
            };
            
            project.GlobalVariables = new List<OeVariable> {
                new OeVariable {
                    Name = "MyCustomVariable",
                    Value = "the value"
                },
                new OeVariable {
                    Name = "second case insensitive",
                    Value = "new {{MyCustomVariable}}"
                }
            };

            project.BuildConfigurations = new List<OeBuildConfiguration> {
                new OeBuildConfiguration {
                    ConfigurationName = "first conf",
                    Variables = new List<OeVariable> {
                        new OeVariable {
                            Name = "buildconfvar1",
                            Value = "val"
                        }
                    },
                    Properties = null,
                    PreBuildStepGroup = new List<OeBuildStepClassic>(),
                    BuildSourceStepGroup = new List<OeBuildStepBuildSource> {
                        new OeBuildStepBuildSource {
                            Label = "step1",
                            Tasks = new List<AOeTask> {
                                new OeTaskFileCompile {
                                    Exclude = "**",
                                    Include = "{{**}}",
                                    TargetDirectory = "mydir"
                                },
                                new OeTaskFileArchiverArchiveProlib {
                                    ExcludeRegex = "regex",
                                    IncludeRegex = "regex",
                                    TargetArchivePath = "myprolib.pl",
                                    TargetDirectory = "insdide/directory",
                                    
                                },
                                new OeTaskFileArchiverArchiveProlibCompile {
                                    ExcludeRegex = "regex",
                                    IncludeRegex = "regex",
                                    TargetArchivePath = "path.zip",
                                    TargetDirectory = "insdide/directory"
                                },
                                new OeTaskFileArchiverArchiveCab {
                                    ExcludeRegex = "regex",
                                    IncludeRegex = "regex",
                                    TargetArchivePath = "myprolib.pl",
                                    TargetDirectory = "inside/directory"
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
                            Label = "step2",
                            Tasks = null
                        }
                    },
                    BuildOutputStepGroup = new List<OeBuildStepClassic> {
                        new OeBuildStepClassic {
                            Label = "step output 1",
                            Tasks = new List<AOeTask> {
                                new OeTaskExec {
                                    Label = "exec1",
                                    ExecutablePath = "exec",
                                    Parameters = "params \"quotes\"",
                                    WorkingDirectory = "dir",
                                    HiddenExecution = false,
                                    IgnoreExitCode = null
                                }
                            }
                        }
                    },
                    PostBuildStepGroup = null
                }
            };
            new OeTaskFileArchiverArchiveProlibCompile {
                ExcludeRegex = "regex",
                IncludeRegex = "regex",
                TargetArchivePath = "path.zip",
                TargetDirectory = "insdide/directory"
            }.Validate();
            project.Save(Path.Combine(TestFolder, "project.xml"));
            
            loadedProject = OeProject.Load(Path.Combine(TestFolder, "project.xml"));
            
            Assert.AreEqual(1, loadedProject.BuildConfigurations[0].BuildSourceStepGroup[0].Tasks[1].Id);

            string xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <GlobalProperties>
    <DlcDirectoryPath></DlcDirectoryPath>
    <ProcedurePathToExecuteBeforeAnyProgressExecution />
  </GlobalProperties>
</Project>
";
            File.WriteAllText(Path.Combine(TestFolder, "input_test.xml"), xmlContent);

            loadedProject = OeProject.Load(Path.Combine(TestFolder, "input_test.xml"));
            
            Assert.AreEqual(@"", loadedProject.GlobalProperties.DlcDirectoryPath);
            Assert.AreEqual(null, loadedProject.GlobalProperties.IniFilePath);
            Assert.AreEqual(@"", loadedProject.GlobalProperties.ProcedurePathToExecuteBeforeAnyProgressExecution);
        }
    }
}