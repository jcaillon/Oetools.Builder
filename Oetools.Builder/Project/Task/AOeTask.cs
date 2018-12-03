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
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// A base task.
    /// </summary>
    public abstract class AOeTask : IOeTask {
        
        /// <summary>
        /// Unique identifier for this task.
        /// </summary>
        [XmlIgnore]
        internal int Id { get; set; }   
        
        /// <summary>
        /// The name of this task. Purely informative.
        /// </summary>
        [XmlAttribute("Name")]
        public string Name { get; set; }
        
        protected ILogger Log { get; set; }
        protected CancellationToken? CancelToken { get; set; }
        protected bool TestMode { get; set; }

        private List<TaskExecutionException> _exceptions;
        private OeProperties ProjectProperties { get; set; }
        
        /// <inheritdoc cref="IOeTask.PublishWarning"/>
        public event EventHandler<TaskWarningEventArgs> PublishWarning;
        
        /// <inheritdoc cref="IOeTask.SetCancelToken"/>
        public void SetCancelToken(CancellationToken? cancelToken) => CancelToken = cancelToken;       
        
        /// <inheritdoc cref="IOeTask.SetTestMode"/>
        public void SetTestMode(bool testMode) => TestMode = testMode;
        
        /// <inheritdoc cref="IOeTask.SetLog"/>
        public void SetLog(ILogger log) => Log = log;
        
        /// <inheritdoc cref="IOeTask.GetRuntimeExceptionList"/>
        public List<TaskExecutionException> GetRuntimeExceptionList() => _exceptions;

        /// <inheritdoc cref="IOeTask.Validate"/>
        public abstract void Validate();
        
        /// <inheritdoc cref="IOeTaskNeedingProperties.SetProperties"/>
        public void SetProperties(OeProperties properties) => ProjectProperties = properties;
        
        /// <inheritdoc cref="IOeTaskNeedingProperties.GetProperties"/>
        public OeProperties GetProperties() => ProjectProperties;

        /// <inheritdoc cref="IOeTask.Execute"/>
        public void Execute() {
            var stopWatch = Stopwatch.StartNew();
            Log?.Debug($"Executing {this}.");
            try {
                if (TestMode) {
                    Log?.Debug("Test mode");
                    ExecuteTestModeInternal();
                } else {
                    ExecuteInternal();
                }
            } catch (OperationCanceledException) {
                throw;
            } catch (TaskExecutionException te) {
                AddExecutionErrorAndThrow(te);
            } catch (Exception e) {
                AddExecutionErrorAndThrow(new TaskExecutionException(this, $"Unexpected error : {e.Message}", e));
            } finally {
                stopWatch.Stop();
                Log?.Debug($"Task ended in {stopWatch.Elapsed.ConvertToHumanTime()}.");
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <remarks>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed.
        /// - This method can publish warnings using <see cref="AOeTask.AddExecutionWarning"/>.
        /// </remarks>
        /// <exception cref="TaskExecutionException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        protected abstract void ExecuteInternal();

        /// <summary>
        /// This method is executed instead of <see cref="ExecuteInternal"/> when test mode is ON.
        /// </summary>
        protected abstract void ExecuteTestModeInternal();

        /// <summary>
        /// Don't use this method, directly throw an <see cref="TaskExecutionException"/> instead.
        /// </summary>
        /// <param name="exception"></param>
        /// <exception cref="TaskExecutionException"></exception>
        protected void AddExecutionErrorAndThrow(TaskExecutionException exception) {
            (_exceptions ?? (_exceptions = new List<TaskExecutionException>())).Add(exception);
            throw exception;
        }

        /// <summary>
        /// Call this method to publish a warning in the task.
        /// </summary>
        /// <param name="exception"></param>
        protected void AddExecutionWarning(TaskExecutionException exception) {
            (_exceptions ?? (_exceptions = new List<TaskExecutionException>())).Add(exception);
            PublishWarning?.Invoke(this, new TaskWarningEventArgs(exception));
        }
        
        /// <summary>
        /// String representation of this task.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Task [{Id}]{(string.IsNullOrEmpty(Name) ? "" : $" {Name}")} of type {GetType().GetXmlName()}";
    }
}