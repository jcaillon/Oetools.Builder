#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (BuilderUtilitiesTest.cs) is part of Oetools.Builder.Test.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Test.Utilities {
    
    [TestClass]
    public class BuilderUtilitiesTest {

        [TestMethod]
        public void ApplyVariablesToProperties_Test() {
            var buildConf = new OeBuildConfiguration {
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        BuildHistoryInputFilePath = "{{var1}}"
                    },
                    CompilationOptions = new OeCompilationOptions {
                        CompilableFileExtensionPattern = "replace stuff {{env{{env2}}}}"
                    }
                },
                BuildSourceTasks = new List<OeBuildStepCompile> {
                    new OeBuildStepCompile {
                        Label = "replace {{anything}} here",
                        Tasks = new List<OeTask> {
                            new OeTaskFileTargetFileCopy {
                                Include = "replace missing '{{missingvar}}' by empty",
                                TargetDirectory = "keep missing '{{missingvar}}' variables!"
                            },
                            new OeTaskFileTargetFileCopy {
                                Exclude = "2 replace missing '{{missingvar}}' by empty",
                                TargetFilePath = "2 keep missing '{{missingvar}}' variables!"
                            }
                        }
                    }
                },
                Variables = new List<OeVariable> {
                    new OeVariable {
                        Name = "first",
                        Value = "var_1{{missing}}"
                    },
                    new OeVariable {
                        Name = "var1",
                        Value = "value_{{first}}"
                    },
                    new OeVariable {
                        Name = "anything",
                        Value = "wtf!"
                    }
                }
            };
            Environment.SetEnvironmentVariable("env2", "1");
            Environment.SetEnvironmentVariable("env1", "value_env_1");
            
            BuilderUtilities.ApplyVariablesInVariables(buildConf.Variables);
            Assert.AreEqual("var_1", buildConf.Variables[0].Value);
            Assert.AreEqual("value_var_1", buildConf.Variables[1].Value);
            Assert.AreEqual("wtf!", buildConf.Variables[2].Value);
            
            BuilderUtilities.ApplyVariablesToProperties(buildConf, buildConf.Variables);
            Assert.AreEqual("value_var_1", buildConf.Properties.BuildOptions.BuildHistoryInputFilePath);
            Assert.AreEqual("replace stuff value_env_1", buildConf.Properties.CompilationOptions.CompilableFileExtensionPattern);
            Assert.AreEqual("replace wtf! here", buildConf.BuildSourceTasks[0].Label);
            Assert.AreEqual("replace missing '' by empty", ((OeTaskFileTargetFileCopy)buildConf.BuildSourceTasks[0].GetTaskList()[0]).Include);
            Assert.AreEqual("keep missing '{{missingvar}}' variables!", ((OeTaskFileTargetFileCopy)buildConf.BuildSourceTasks[0].GetTaskList()[0]).TargetDirectory);
            Assert.AreEqual("2 replace missing '' by empty", ((OeTaskFileTargetFileCopy)buildConf.BuildSourceTasks[0].GetTaskList()[1]).Exclude);
            Assert.AreEqual("2 keep missing '{{missingvar}}' variables!", ((OeTaskFileTargetFileCopy)buildConf.BuildSourceTasks[0].GetTaskList()[1]).TargetFilePath);
            
        }
    }
}