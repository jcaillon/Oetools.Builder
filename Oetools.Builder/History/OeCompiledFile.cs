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
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.History {
    
    [Serializable]
    public class OeCompiledFile : IPathListItem {
            
        /// <summary>
        /// Path of the file compiled
        /// </summary>
        [XmlAttribute(AttributeName ="SourceFilePath")]
        [BaseDirectory(Type = BaseDirectoryType.SourceDirectory)]
        public string Path { get; set; }
            
        [XmlArray("CompilationProblems")]
        [XmlArrayItem("Error", typeof(OeCompilationError))]
        [XmlArrayItem("Warning", typeof(OeCompilationWarning))]
        public List<AOeCompilationProblem> CompilationProblems { get; set; }
    }
}