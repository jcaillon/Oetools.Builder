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

using DotUtilities;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// A task that operates on directories.
    /// </summary>
    public interface IOeTaskDirectory : IOeTaskFilter {

        /// <summary>
        /// Sets the list of directories to be processed by this task.
        /// </summary>
        /// <param name="pathsToBuild"></param>
        void SetDirectoriesToProcess(PathList<IOeDirectory> pathsToBuild);

        /// <summary>
        /// Gets a list of directories to be processed by this task.
        /// </summary>
        /// <returns></returns>
        PathList<IOeDirectory> GetDirectoriesToProcess();

        /// <summary>
        /// Given the inclusion wildcard paths and exclusion patterns, returns a list of directories on which to apply this task.
        /// This is when the pattern can describe a directory or directories.
        /// </summary>
        /// <example>
        /// If the include pattern is directly a file path.
        /// Of if the include pattern is something like C:\windows\exe*
        /// </example>
        /// <returns></returns>
        PathList<IOeDirectory> GetDirectoriesToProcessFromIncludes();

        /// <summary>
        /// Validates that the task can be applied on directories without having a base directory to list; for that,
        /// the task must have an included path (and should not use regex inclusion).
        /// </summary>
        /// <example>
        /// See the examples in <see cref="GetDirectoriesToProcessFromIncludes"/>
        /// </example>
        /// <exception cref="TaskExecutionException"></exception>
        void ValidateCanGetDirectoriesToProcessFromIncludes();

    }
}
