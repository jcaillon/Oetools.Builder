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
        public void Serialization_Test() {

            var project = OeProject.GetDefaultProject();
            var xmlPath = Path.Combine(TestFolder, "project.xml");
            Utils.CreateDirectoryIfNeeded(TestFolder);
           
            project.BuildConfigurations[0].BuildSourceTasks[0].GetTaskList().AddRange(new List<OeTask> {
                new OeTaskCopy() {
                    Label = "copy"
                },
                new OeTaskZip() {
                    Label = "Zip"
                },
                new OeTaskCompile() {
                    Label = "zsdze"
                },
                new OeTaskProlib() {
                    Label = "derp"
                },
                new OeTaskCompileProlib() {
                    Label = "derp"
                },
                new OeTaskCab() {
                    Label = "derp"
                },
                new OeTaskCompileCab() {
                    Label = "derp"
                }
            });
            
            project.Save(xmlPath);
            
            
        }
    }
}