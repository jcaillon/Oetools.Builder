#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (AOeTaskTest.cs) is part of Oetools.Builder.Test.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Test.Project.Task {
    
    [TestClass]
    public class AOeTaskTest {

        [TestMethod]
        public void Execute() {
            var task = new AOeTask2();
            task.Execute();
            Assert.AreEqual(1, task.Executed);
            
            task = new AOeTask2();
            task.SetTestMode(true);
            task.Execute();
            Assert.AreEqual(1, task.ExecutedTest);
            Assert.AreEqual(0, task.GetRuntimeExceptionList()?.Count ?? 0);

            task = new AOeTask2();
            task.ShouldPublishWarning = true;
            task.Execute();
            Assert.AreEqual(1, task.GetRuntimeExceptionList()?.Count ?? 0);
            
            task = new AOeTask2();
            task.GetException = () => new Exception("derp");
            Assert.ThrowsException<TaskExecutionException>(() => task.Execute());
            Assert.AreEqual(1, task.GetRuntimeExceptionList()?.Count ?? 0);
        }

        private class AOeTask2 : AOeTask {
            
            public bool ShouldPublishWarning { get; set; }
            public Func<Exception> GetException { get; set; }
            public int Executed { get; set; }
            public int ExecutedTest { get; set; }
            
            public override void Validate() { }

            protected override void ExecuteInternal() {
                OnExecute();
                Executed++;
            }

            protected override void ExecuteTestModeInternal() {
                OnExecute();
                ExecutedTest++;
            }

            private void OnExecute() {
                var ex = GetException?.Invoke();
                if (ex != null) {
                    throw ex;
                }
                if (ShouldPublishWarning) {
                    AddExecutionWarning(new TaskExecutionException(this, "coucou"));
                }
            }
        }
    }
}