#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeTaskCompile.cs) is part of Oetools.Builder.
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
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// A task that allows to compile files.
    /// </summary>
    public interface IOeTaskCompile : IOeTaskFileToBuild, IOeTaskNeedingProperties {

        /// <summary>
        /// Sets a list of files that were compiled for this task.
        /// </summary>
        /// <param name="compiledPath"></param>
        void SetCompiledFiles(PathList<UoeCompiledFile> compiledPath);

        /// <summary>
        /// Gets a list of files that were compiled for this task.
        /// </summary>
        /// <returns></returns>
        PathList<UoeCompiledFile> GetCompiledFiles();

    }
}
