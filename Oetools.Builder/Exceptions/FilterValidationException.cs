﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (FilterValidationException.cs) is part of Oetools.Builder.
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
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Exceptions {
    
    public class FilterValidationException : BuilderException {
        public int FilterNumber { get; set; }
        public FilterValidationException(string message) : base(message) { }
        public FilterValidationException(string message, Exception innerException) : base(message, innerException) { }
        public override string Message => $"Filter expression [{FilterNumber.ToString()}]: {base.Message ?? "null"}";
    }
}