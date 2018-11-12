#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeTask.cs) is part of Oetools.Builder.
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
using Oetools.Builder.Exceptions;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// A task to execute.
    /// </summary>
    public interface IOeTask {

        /// <summary>
        /// Validates that the task is correct (correct parameters and can execute).
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        void Validate();

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <remarks>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed
        /// - This method can publish warnings using <see cref="AOeTask.AddExecutionWarning"/>
        /// </remarks>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="TaskExecutionException"></exception>
        void Execute();

        /// <summary>
        /// Returns a list of exceptions thrown during the execution of this task.
        /// </summary>
        /// <returns></returns>
        List<TaskExecutionException> GetRuntimeExceptionList();

        /// <summary>
        /// Event published when an exception happens in a task, should be used to display those errors to the user
        /// (in case of warnings, the user might have chose to no stop the build on them, but we still want to show them to him immediately).
        /// </summary>
        event EventHandler<TaskWarningEventArgs> PublishWarning;
        
        /// <summary>
        /// Sets a logger instance.
        /// </summary>
        /// <param name="log"></param>
        void SetLog(ILogger log);
        
        /// <summary>
        /// Sets whether or not this task should execute in test mode.
        /// </summary>
        /// <param name="testMode"></param>
        void SetTestMode(bool testMode);

        /// <summary>
        /// Sets the <see cref="CancellationToken"/> that will allow this task to be cancelled.
        /// </summary>
        /// <param name="cancelToken"></param>
        void SetCancelToken(CancellationToken? cancelToken);

    }
}