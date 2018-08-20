#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExceptionEventArgs.cs) is part of Oetools.Builder.
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
using Oetools.Builder.Exceptions;

namespace Oetools.Builder.Utilities {
    public class TaskExceptionEventArgs : EventArgs {
        
        /// <summary>
        /// True if the exception should be considered as a warning only
        /// </summary>
        public bool IsWarning { get; set; }
        
        public TaskExecutionException Exception { get; set; }

        public TaskExceptionEventArgs(bool isWarning, TaskExecutionException exception) {
            IsWarning = isWarning;
            Exception = exception;
        }
    }
}