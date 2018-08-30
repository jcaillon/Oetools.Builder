﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutorException.cs) is part of Oetools.Builder.
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

namespace Oetools.Builder.Exceptions {
    public class TaskExecutorException : BuilderException {
        
        public BuildStepExecutor BuildStepExecutor { get; }
        
        public TaskExecutorException(BuildStepExecutor buildStepExecutor, string message) : base(message) {
            BuildStepExecutor = buildStepExecutor;
        }
        public TaskExecutorException(BuildStepExecutor buildStepExecutor, string message, Exception innerException) : base(message, innerException) {
            BuildStepExecutor = buildStepExecutor;
        }
        
        public override string Message => $"{BuildStepExecutor?.ToString() ?? ""} : {base.Message ?? ""}";
    }
}