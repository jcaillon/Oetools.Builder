#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeBuildConfigurationTest.cs) is part of Oetools.Builder.Test.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Openedge;

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class OeBuildConfigurationTest {

        [TestMethod]
        public void ApplyVariables_DefaultVariables() {
            var bc = new OeBuildConfiguration();
            bc.ApplyVariables();
            
            Assert.IsTrue(bc.Variables.Exists(v => v.Name.Equals(OeBuilderConstants.OeVarNameSourceDirectory)));
            Assert.IsTrue(bc.Variables.Exists(v => v.Name.Equals(OeBuilderConstants.OeVarNameProjectDirectory)));
            Assert.IsTrue(bc.Variables.Exists(v => v.Name.Equals(OeBuilderConstants.OeVarNameProjectLocalDirectory)));
            Assert.IsTrue(bc.Variables.Exists(v => v.Name.Equals(UoeConstants.OeDlcEnvVar)));
            Assert.IsTrue(bc.Variables.Exists(v => v.Name.Equals(OeBuilderConstants.OeVarNameOutputDirectory)));
            Assert.IsTrue(bc.Variables.Exists(v => v.Name.Equals(OeBuilderConstants.OeVarNameConfigurationName)));
            Assert.IsTrue(bc.Variables.Exists(v => v.Name.Equals(OeBuilderConstants.OeVarNameCurrentDirectory)));
        }
        
        
        [TestMethod]
        public void SetDefaultValues() {
            var bc = new OeBuildConfiguration();
            bc.SetDefaultValues();
            Assert.IsNotNull(bc.Properties);
            Assert.AreEqual(PathListerGitFilterOptions.GetDefaultIncludeSourceFilesCommittedOnlyOnCurrentBranch(), bc.Properties.BuildOptions.SourceToBuildGitFilter.IncludeSourceFilesCommittedOnlyOnCurrentBranch);
            Assert.AreEqual(OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput(), bc.Properties.BuildOptions?.IncrementalBuildOptions?.MirrorDeletedSourceFileToOutput);
            Assert.AreEqual(OeCompilationOptions.GetDefaultCompileWithDebugList(), bc.Properties.CompilationOptions.CompileWithDebugList);
            Assert.AreEqual(OeBuildOptions.GetDefaultStopBuildOnTaskWarning(), bc.Properties.BuildOptions.StopBuildOnTaskWarning);

            Assert.IsNotNull(bc.Properties.BuildOptions);
            Assert.IsNotNull(bc.Properties.BuildOptions.IncrementalBuildOptions);
            Assert.IsNotNull(bc.Properties.BuildOptions.IncrementalBuildOptions.MirrorDeletedTargetsToOutput);
            Assert.IsNotNull(bc.Properties.CompilationOptions);
            
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            Assert.AreEqual(OeProperties.GetDefaultDlcDirectoryPath(), bc.Properties.DlcDirectoryPath);
        }
        
        [TestMethod]
        public void ApplyVariables() {
            var bc = new OeBuildConfiguration {
                Variables = new List<OeVariable> {
                    new OeVariable {
                        Name = "var1",
                        Value = "value3"
                    },
                    new OeVariable {
                        Name = "var2",
                        Value = "{{var1}}"
                    },
                    new OeVariable {
                        Name = "var3",
                        Value = "{{var2}}"
                    },
                    new OeVariable {
                        Name = "var4",
                        Value = "value4"
                    },
                    new OeVariable {
                        Name = "var4",
                        Value = "value4-bis"
                    },
                    new OeVariable {
                        Name = OeBuilderConstants.OeVarNameCurrentDirectory,
                        Value = "value-cd"
                    }
                },
                Properties = new OeProperties {
                    ProcedureToExecuteAfterAnyProgressExecutionFilePath = "{{var4}}",
                    ProcedureToExecuteBeforeAnyProgressExecutionFilePath = "{{var1}}",
                    IniFilePath = "{{" + OeBuilderConstants.OeVarNameCurrentDirectory + "}}"
                }
            };
            
            bc.ApplyVariables();

            Assert.AreEqual("value4-bis", bc.Properties.ProcedureToExecuteAfterAnyProgressExecutionFilePath, "Should take the last defined value for identical variables.");
            Assert.AreEqual("value3", bc.Properties.ProcedureToExecuteBeforeAnyProgressExecutionFilePath, "Should have replaced variables within variables.");
            Assert.AreEqual("value-cd", bc.Properties.IniFilePath, "Default variables do not have the priority.");
            
        }
    }
}