#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutorTest.cs) is part of Oetools.Builder.Test.
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
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Test.Utilities {
    
    [TestClass]
    public class ProjectDatabaseAdministratorTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(ProjectDatabaseAdministratorTest)));
                     
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
        public void ProjectDatabaseAdministrator_Test() {
            if (!TestHelper.GetDlcPath(out string dlcPath)) {
                return;
            }       
            
            // create .df files
            var dfcontent = "ADD SEQUENCE \"sequence1\"\n  INITIAL 0\n  INCREMENT 1\n  CYCLE-ON-LIMIT no\n\nADD TABLE \"table1\"\n  AREA \"Schema Area\"\n  DESCRIPTION \"table one\"\n  DUMP-NAME \"table1\"\n\nADD FIELD \"field1\" OF \"table1\" AS character \n  DESCRIPTION \"field one\"\n  FORMAT \"x(8)\"\n  INITIAL \"\"\n  POSITION 2\n  MAX-WIDTH 16\n  ORDER 10\n\nADD INDEX \"idx_1\" ON \"table1\" \n  AREA \"Schema Area\"\n  PRIMARY\n  INDEX-FIELD \"field1\" ASCENDING";
            var dfcontent2 = "ADD SEQUENCE \"sequence2\"\n  INITIAL 0\n  INCREMENT 1\n  CYCLE-ON-LIMIT no\n\nADD TABLE \"table2\"\n  AREA \"Schema Area\"\n  DESCRIPTION \"table one\"\n  DUMP-NAME \"table2\"\n\nADD FIELD \"field1\" OF \"table2\" AS character \n  DESCRIPTION \"field one\"\n  FORMAT \"x(8)\"\n  INITIAL \"\"\n  POSITION 2\n  MAX-WIDTH 16\n  ORDER 10\n\nADD INDEX \"idx_1\" ON \"table2\" \n  AREA \"Schema Area\"\n  PRIMARY\n  INDEX-FIELD \"field1\" ASCENDING";
            var dfPath1 = Path.Combine(TestFolder, "db1.df");
            var dfPath2 = Path.Combine(TestFolder, "db2.df");
            var dfPath3 = Path.Combine(TestFolder, "db3.df");
            
            File.WriteAllText(dfPath1, dfcontent);
            File.WriteAllText(dfPath2, dfcontent);
            File.WriteAllText(dfPath3, dfcontent2);

            var build = new OeBuildConfiguration {
                Properties = new OeProperties {
                    DlcDirectoryPath = dlcPath,
                    ProjectDatabases = new List<OeProjectDatabase> {
                        new OeProjectDatabase {
                            LogicalName = "db1",
                            DataDefinitionFilePath = dfPath1
                        },
                        new OeProjectDatabase {
                            LogicalName = "db2",
                            DataDefinitionFilePath = dfPath2
                        }
                    }
                },
                Id = 1
            };
            
            var env = new UoeExecutionEnv {
                DlcDirectoryPath = dlcPath
            };

            var sourceDirectory = Path.Combine(TestFolder, "source");
            Utils.CreateDirectoryIfNeeded(sourceDirectory);
            
            // setup databases
            using (var dbAdmin = new ProjectDatabaseAdministrator(build, sourceDirectory)) {
                var connectionString = string.Join(' ', dbAdmin.SetupProjectDatabases());
                Assert.IsFalse(string.IsNullOrEmpty(connectionString));

                env.DatabaseConnectionString = connectionString;
                using (var exec = new UoeExecutionDbExtractTableAndSequenceList(env)) {
                    exec.Start();
                    exec.WaitForExecutionEnd();
                    Assert.IsFalse(exec.ExecutionHandledExceptions, "ExecutionHandledExceptions");
                    Assert.IsTrue(exec.TablesCrc.Keys.ToList().Exists(t => t.EndsWith("table1")));
                    Assert.IsFalse(exec.TablesCrc.Keys.ToList().Exists(t => t.EndsWith("table2")));
                }
            }
            
            // call again, should basically do nothing
            using (var dbAdmin = new ProjectDatabaseAdministrator(build, sourceDirectory)) {
                var connectionString = string.Join(' ', dbAdmin.SetupProjectDatabases());
                
                env.DatabaseConnectionString = connectionString;
                using (var exec = new UoeExecutionDbExtractTableAndSequenceList(env)) {
                    exec.Start();
                    exec.WaitForExecutionEnd();
                    Assert.IsFalse(exec.ExecutionHandledExceptions, "ExecutionHandledExceptions");
                }
            }
            
            // now change the .df, should delete and recreate the db2
            build.Properties.ProjectDatabases[1].DataDefinitionFilePath = dfPath3;
            using (var dbAdmin = new ProjectDatabaseAdministrator(build, sourceDirectory)) {
                var connectionString = string.Join(' ', dbAdmin.SetupProjectDatabases());
                Assert.IsFalse(string.IsNullOrEmpty(connectionString));

                env.DatabaseConnectionString = connectionString;
                using (var exec = new UoeExecutionDbExtractTableAndSequenceList(env)) {
                    exec.Start();
                    exec.WaitForExecutionEnd();
                    Assert.IsFalse(exec.ExecutionHandledExceptions, "ExecutionHandledExceptions");
                    Assert.IsTrue(exec.TablesCrc.Keys.ToList().Exists(t => t.EndsWith("table2")));
                }
            }
            
            // shutdown all
            using (var dbAdmin = new ProjectDatabaseAdministrator(build, sourceDirectory)) {
                dbAdmin.ShutdownAllDatabases();
            }

            Utils.DeleteDirectoryIfExists(sourceDirectory, true);
        }


    }
}