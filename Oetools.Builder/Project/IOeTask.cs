﻿#region header
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

namespace Oetools.Builder.Project {
    public interface IOeTask {

        /// <summary>
        /// Executes the task
        /// </summary>
        /// <remarks>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed
        /// - This method can publish warnings using <see cref="OeTask.AddExecutionWarning"/>
        /// </remarks>
        /// <exception cref="TaskExecutionException"></exception>
        void Execute();

        List<TaskExecutionException> GetExceptionList();

        event EventHandler<TaskExceptionEventArgs> PublishException;
        
        void SetLog(ILogger log);

        void SetCancelSource(CancellationTokenSource cancelSource);

    }
}