#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeCompilationProblem.cs) is part of Oetools.Builder.
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
using System.Xml.Serialization;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.History {
    
    public abstract class AOeCompilationProblem {
            
        /// <summary>
        /// Path of the file in which we found the error
        /// </summary>
        [XmlAttribute(AttributeName ="FilePath")]
        [BaseDirectory(Type = BaseDirectoryType.SourceDirectory)]
        public string FilePath { get; set; }
            
        [XmlAttribute(AttributeName ="Line")]
        public int Line { get; set; }
            
        [XmlAttribute(AttributeName ="Column")]
        public int Column { get; set; }
            
        [XmlAttribute(AttributeName ="ErrorNumber")]
        public int ErrorNumber { get; set; }
            
        [XmlAttribute(AttributeName ="Message")]
        public string Message { get; set; }
        
        internal static AOeCompilationProblem New(UoeCompilationProblem  problem) {
            AOeCompilationProblem output;
            switch (problem) {
                case UoeCompilationError _:
                    output = new OeCompilationError();
                    break;
                case UoeCompilationWarning _:
                    output = new OeCompilationWarning();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(UoeCompilationProblem), problem, "no matching type");
            }
            Utils.DeepCopyPublicProperties(problem, output.GetType(), output);
            return output;
        }
    }
}