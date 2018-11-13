#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TargetValidationException.cs) is part of Oetools.Builder.
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
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Exceptions {
    
    public class TargetValidationException : TaskValidationException {
        public int TargetNumber { get; set; }
        public TargetValidationException(AOeTask task, string message) : base(task, message) { }
        public TargetValidationException(AOeTask task, string message, Exception innerException) : base(task, message, innerException) { }
        public override string Message => $"Target {TargetNumber.ToString()}: {base.Message ?? ""}";
    }
}