#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeCompilationProblemTest.cs) is part of Oetools.Builder.Test.
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.History;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Test.History {
    
    [TestClass]
    public class OeCompilationProblemTest {

        [TestMethod]
        public void OeCompilationProblem_Test_New() {
            var new1 = OeCompilationProblem.New("compiled", new UoeCompilationWarning {
                Message = "message",
                Column = 2,
                Line = 2,
                ErrorNumber = 2,
                SourceFilePath = "source"
            });
            Assert.AreEqual(typeof(OeCompilationWarning), new1.GetType());
            Assert.AreEqual("compiled", new1.CompiledSourceFilePath);
            Assert.AreEqual("message", new1.Message);
            Assert.AreEqual("source", new1.SourceFilePath);
            Assert.AreEqual(2, new1.Column);
            Assert.AreEqual(2, new1.Line);
            Assert.AreEqual(2, new1.ErrorNumber);
            
            
            var new2 = OeCompilationProblem.New("compiled2", new UoeCompilationError {
                Message = "message2",
                Column = 3,
                Line = 3,
                ErrorNumber = 3,
                SourceFilePath = "source2"
            });
            Assert.AreEqual(typeof(OeCompilationError), new2.GetType());
            Assert.AreEqual("compiled2", new2.CompiledSourceFilePath);
            Assert.AreEqual("message2", new2.Message);
            Assert.AreEqual("source2", new2.SourceFilePath);
            Assert.AreEqual(3, new2.Column);
            Assert.AreEqual(3, new2.Line);
            Assert.AreEqual(3, new2.ErrorNumber);
            
        }
        
    }
}