#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ReplacePlaceHolder.cs) is part of Oetools.Builder.
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
using Oetools.Utilities.Lib.Attributes;

namespace Oetools.Builder.Utilities.Attributes {
    
    /// <summary>
    /// Special attribute that allows to decide wether or not variables should be replaced in a property of type string
    /// and wether or not it should be replaced by an empty string (or left as is) if the variable value is not found
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReplaceVariablesAttribute : ReplaceStringPropertyAttribute {
            
        /// <summary>
        /// Replace unknown values by an empty string
        /// </summary>
        public bool LeaveUnknownUntouched { get; set;  }
    }
}