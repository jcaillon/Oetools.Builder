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

namespace Oetools.Builder.Project {
    
    public abstract class OeTask : IOeTask {
        
        public event EventHandler<TaskExceptionEventArgs> PublishException;
        
        protected CancellationTokenSource CancelSource { get; set; }
        
        protected ILogger Log { get; set; }

        private List<TaskExecutionException> _exceptions;
        
        /// <summary>
        /// Validates that the task is correct (correct parameters and can execute)
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public virtual void Validate() { }
        
        [XmlAttribute("Label")]
        public string Label { get; set; }
        
        [XmlIgnore]
        internal int Id { get; set; }

        public void Execute() {
            try {
                ExecuteInternal();
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception e) {
                AddExecutionError(new TaskExecutionException(this, $"Unexpected error in task {ToString().PrettyQuote()} : {e.Message}", e));
            }
        }

        protected virtual void ExecuteInternal() {
            throw new NotImplementedException();
        }

        public void SetLog(ILogger log) {
            Log = log;
        }

        public void SetCancelSource(CancellationTokenSource cancelSource) {
            CancelSource = cancelSource;
        }

        public List<TaskExecutionException> GetExceptionList() {
            return _exceptions;
        }

        protected void AddExecutionError(TaskExecutionException exception) {
            (_exceptions ?? (_exceptions = new List<TaskExecutionException>())).Add(exception);
            PublishException?.Invoke(this, new TaskExceptionEventArgs(false, exception));
        }

        protected void AddExecutionWarning(TaskExecutionException exception) {
            (_exceptions ?? (_exceptions = new List<TaskExecutionException>())).Add(exception);
            PublishException?.Invoke(this, new TaskExceptionEventArgs(true, exception));
        }
        
        public override string ToString() => $"Task [{Id}]{(string.IsNullOrEmpty(Label) ? "" : $" {Label}")} of type {GetType().GetXmlName()}";
    }
}