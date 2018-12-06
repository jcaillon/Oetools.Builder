#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeFileToBuildTargetArchive.cs) is part of Oetools.Builder.
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
using System.Collections.Generic;

namespace Oetools.Builder.History {
    
    /// <summary>
    /// Represents a file that has been built.
    /// </summary>
    public interface IOeFileBuilt : IOeFile {
        
        /// <summary>
        /// The list of targets built.
        /// </summary>
        List<AOeTarget> Targets { get; set; }
        
        /// <summary>
        /// Represents all the source files that were used when compiling the original source file
        /// (for instance includes or interfaces)
        /// </summary>
        List<string> RequiredFiles { get; set; }
        
        /// <summary>
        /// Represents all the database entities referenced in the original source file and used for the compilation
        /// (can be sequences or tables)
        /// </summary>
        List<OeDatabaseReference> RequiredDatabaseReferences { get; set; }
        
        /// <summary>
        /// Compilations problems for this compiled file.
        /// </summary>
        List<AOeCompilationProblem> CompilationProblems { get; set; }
    }
}