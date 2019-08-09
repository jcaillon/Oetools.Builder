#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (CompilerException.cs) is part of Oetools.Builder.
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
using System.Text;
using Oetools.Utilities.Openedge.Execution.Exceptions;

namespace Oetools.Builder.Exceptions {
    public class CompilerException : Exception {
        
        public List<UoeExecutionException> HandledExceptions { get; }
        
        public CompilerException(List<UoeExecutionException> handledExceptions) {
            HandledExceptions = handledExceptions;
        }
        
        public override string Message {
            get {
                var sb = new StringBuilder("Compiler exceptions: ");
                if (HandledExceptions != null && HandledExceptions.Count > 0) {
                    foreach (var ex in HandledExceptions) {
                        sb.AppendLine();
                        sb.Append("* ").Append(ex.Message);
                    }
                } else {
                    sb.Append("empty exception list.");
                }
                return sb.ToString();
            }
        }
        
    }
}