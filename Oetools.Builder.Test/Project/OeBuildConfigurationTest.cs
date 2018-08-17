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

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class OeBuildConfigurationTest {

        public void Test() {
            var project = OeProject.GetDefaultProject();
            var bc = project.GetDefaultBuildConfigurationCopy();
            
            bc.PostBuildTasks = new List<OeBuildStepClassic> {
                new OeBuildStepClassic {
                    Tasks = new List<OeTask> {
                        new OeTaskProlib() {
                            
                        }
                    }
                }
            };
            
            bc.Variables = new List<OeVariable> {
                new OeVariable {
                    Name = "cool",
                    Value = "<zefzef"
                }
            };
            
            bc.ApplyVariables("directory");
            

        }
    }
}