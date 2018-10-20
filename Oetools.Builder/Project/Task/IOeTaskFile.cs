#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeTaskFile.cs) is part of Oetools.Builder.
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

using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// A task that operates on files.
    /// </summary>
    public interface IOeTaskFile : IOeTaskFilter {

        /// <summary>
        /// Sets the list of files to be built by this task.
        /// </summary>
        /// <param name="pathsToBuild"></param>
        void SetFilesToBuild(PathList<OeFile> pathsToBuild);
        
        /// <summary>
        /// Gets a list of files to be built by this task.
        /// </summary>
        /// <returns></returns>
        PathList<OeFile> GetFilesToBuild();
        
        /// <summary>
        /// Given the inclusion wildcard paths and exclusion patterns, returns a list of files on which to apply this task.
        /// This is when the pattern can describe a file or files.
        /// </summary>
        /// <example>
        /// If the include pattern is directly a file path.
        /// Of if the include pattern is something like C:\windows\exe*
        /// </example>
        /// <returns></returns>
        PathList<OeFile> GetFilesToBuildFromIncludes();

        /// <summary>
        /// Validates that the task can be applied on files without having a base directory to list; for that,
        /// the task must have an included path (and should not use regex inclusion).
        /// </summary>
        /// <example>
        /// See the examples in <see cref="GetFilesToBuildFromIncludes"/>
        /// </example>
        /// <exception cref="TaskExecutionException"></exception>
        void ValidateCanGetFilesToBuildFromIncludes();
        
    }
}