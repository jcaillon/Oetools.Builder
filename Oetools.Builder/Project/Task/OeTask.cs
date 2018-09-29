#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTask.cs) is part of Oetools.Builder.
// 
// Oetools.Builder is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    public abstract class OeTask : IOeTask {
        
        [XmlAttribute("Label")]
        public string Label { get; set; }
        
        [XmlIgnore]
        internal int Id { get; set; }   
        
        public event EventHandler<TaskWarningEventArgs> PublishWarning;
        
        protected CancellationTokenSource CancelSource { get; set; }
        public void SetCancelSource(CancellationTokenSource cancelSource) => CancelSource = cancelSource;

        protected bool TestMode { get; set; }
        public void SetTestMode(bool testMode) => TestMode = testMode;
        
        protected ILogger Log { get; set; }
        public void SetLog(ILogger log) => Log = log;

        private List<TaskExecutionException> _exceptions;
        public List<TaskExecutionException> GetExceptionList() => _exceptions;
        
        /// <inheritdoc cref="IOeTask.Validate"/>
        public virtual void Validate() { }
        
        private OeProperties ProjectProperties { get; set; }
        public void SetProperties(OeProperties properties) => ProjectProperties = properties;
        public OeProperties GetProperties() => ProjectProperties;

        /// <summary>
        /// Main entry point for simple execution tasks
        /// </summary>
        public void Execute() {
            Log?.Debug($"Executing {this}");
            try {
                if (TestMode) {
                    Log?.Debug("Test mode");
                    ExecuteTestModeInternal();
                } else {
                    ExecuteInternal();
                }
            } catch (OperationCanceledException) {
                throw;
            } catch (TaskExecutionException) {
                throw;
            } catch (Exception e) {
                AddExecutionErrorAndThrow(new TaskExecutionException(this, $"Unexpected error : {e.Message}", e));
            }
        }

        /// <summary>
        /// Executes the task
        /// </summary>
        /// <remarks>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed
        /// - This method can publish warnings using <see cref="OeTask.AddExecutionWarning"/>
        /// </remarks>
        /// <exception cref="TaskExecutionException"></exception>
        protected virtual void ExecuteInternal() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds all the files to build to the built files list,
        /// this method is executed instead of <see cref="ExecuteInternal"/> when test mode is on
        /// </summary>
        protected virtual void ExecuteTestModeInternal() { }

        /// <summary>
        /// Don't use this method, directly throw an <see cref="TaskExecutionException"/> instead
        /// </summary>
        /// <param name="exception"></param>
        /// <exception cref="TaskExecutionException"></exception>
        protected void AddExecutionErrorAndThrow(TaskExecutionException exception) {
            (_exceptions ?? (_exceptions = new List<TaskExecutionException>())).Add(exception);
            throw exception;
        }

        /// <summary>
        /// Call this method to publish a warning in the task
        /// </summary>
        /// <param name="exception"></param>
        protected void AddExecutionWarning(TaskExecutionException exception) {
            (_exceptions ?? (_exceptions = new List<TaskExecutionException>())).Add(exception);
            PublishWarning?.Invoke(this, new TaskWarningEventArgs(exception));
        }
        
        public override string ToString() => $"Task [{Id}]{(string.IsNullOrEmpty(Label) ? "" : $" {Label}")} of type {GetType().GetXmlName()}";
    }
}