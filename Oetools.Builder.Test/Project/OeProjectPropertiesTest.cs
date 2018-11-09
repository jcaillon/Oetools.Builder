#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeProjectPropertiesTest.cs) is part of Oetools.Builder.Test.
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
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class OeProjectPropertiesTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(OeProjectPropertiesTest)));
                     
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
        public void SetDefaultValues_Test() {
            var prop = new OeProperties();
            prop.SetDefaultValues();
            Assert.AreEqual(OeGitFilterOptions.GetDefaultOnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch(), prop.SourceToBuildGitFilterOptions.OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch);
            Assert.AreEqual(OeIncrementalBuildOptions.GetDefaultMirrorDeletedSourceFileToOutput(), prop.IncrementalBuildOptions.MirrorDeletedSourceFileToOutput);
            Assert.AreEqual(OeCompilationOptions.GetDefaultCompileWithDebugList(), prop.CompilationOptions.CompileWithDebugList);
            Assert.AreEqual(OeBuildOptions.GetDefaultTreatWarningsAsErrors(), prop.BuildOptions.TreatWarningsAsErrors);
            
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            Assert.AreEqual(OeProperties.GetDefaultDlcDirectoryPath(), prop.DlcDirectoryPath);
        }

        [TestMethod]
        public void GetPropath_Test() {
            if (!TestHelper.GetDlcPath(out string _)) {
                return;
            }
            
            var iniPath = !Utils.IsRuntimeWindowsPlatform ? null : Path.Combine(TestFolder, "test.ini");
            if (!string.IsNullOrEmpty(iniPath)) {
                File.WriteAllText(iniPath, "[Startup]\nPROPATH=t:\\error:exception\";C:\\Windows,%TEMP%;z:\\no");
            }

            Directory.CreateDirectory(Path.Combine(TestFolder, "test1"));
            Directory.CreateDirectory(Path.Combine(TestFolder, "test2"));
            Directory.CreateDirectory(Path.Combine(TestFolder, ".git", "subtest2"));
            Directory.CreateDirectory(Path.Combine(TestFolder, ".git", "subtest2", "end2"));
            Directory.CreateDirectory(Path.Combine(TestFolder, "test3"));
            Directory.CreateDirectory(Path.Combine(TestFolder, "test3", "subtest3"));
            var dirInfo = Directory.CreateDirectory(Path.Combine(TestFolder, "test1_hidden"));
            dirInfo.Attributes |= FileAttributes.Hidden;

            var prop = new OeProperties {
                PropathEntries = !Utils.IsRuntimeWindowsPlatform ? null : new List<string> {
                    "{{DLC}}",
                    "C:\\Windows\\System32",
                    "C:\\Windows\\System32\\drivers",
                    "test1"
                },
                IniFilePath = iniPath,
                AddAllSourceDirectoriesToPropath = false,
                PropathSourceDirectoriesFilter = null,
                AddDefaultOpenedgePropath = false
            };

            BuilderUtilities.ApplyVariablesToProperties(prop, null);

            var list = prop.GetPropath(TestFolder, false);
            Assert.AreEqual(2 + (Utils.IsRuntimeWindowsPlatform ? 4 : 0), list.Count);
            Assert.IsTrue(list.Contains(Environment.GetEnvironmentVariable("dlc").ToCleanPath()));

            prop.AddAllSourceDirectoriesToPropath = true;
            
            list = prop.GetPropath(TestFolder, false);
            Assert.AreEqual(6 + (Utils.IsRuntimeWindowsPlatform ? 4 : 0), list.Count);
            Assert.IsTrue(list.Contains(Path.Combine(TestFolder, "test3", "subtest3")));
            
            prop.PropathSourceDirectoriesFilter = new OeFilterOptions {
                Exclude = "**sub**",
                ExcludeRegex = "[hH][Ii][Dd]"
            };
            
            list = prop.GetPropath(TestFolder, false);
            Assert.AreEqual(4 + (Utils.IsRuntimeWindowsPlatform ? 4 : 0), list.Count);
            
            list = prop.GetPropath(TestFolder, true);
            Assert.AreEqual(4 + (Utils.IsRuntimeWindowsPlatform ? 4 : 0), list.Count);
            Assert.IsTrue(list.Contains("test3"));
            
            if (!TestHelper.GetDlcPath(out string dlcPath)) {
                return;
            }

            prop.DlcDirectoryPath = dlcPath;
            prop.AddDefaultOpenedgePropath = true;
            
            list = prop.GetPropath(TestFolder, false);
            Assert.IsTrue(list.Count > 7 + (Utils.IsRuntimeWindowsPlatform ? 4 : 0));
        }
        
    }
}