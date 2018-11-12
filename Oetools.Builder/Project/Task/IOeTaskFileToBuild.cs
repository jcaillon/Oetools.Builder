#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeTaskFileTarget.cs) is part of Oetools.Builder.
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

using Oetools.Builder.History;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// A task that operates on files that need targets.
    /// </summary>
    public interface IOeTaskFileToBuild : IOeTaskFile {
        
        /// <summary>
        /// Sets the base directory that will be used to determine the target of each file.
        /// </summary>
        /// <param name="baseDirectory"></param>
        void SetBaseDirectory(string baseDirectory);

        /// <summary>
        /// Gets the list of files to build.
        /// </summary>
        /// <returns></returns>
        PathList<IOeFileToBuild> GetFilesToBuild();
        
        /// <summary>
        /// For a given list of files, set the targets that will be needed from this task.
        /// Relative path are transformed into absolute path using <paramref name="baseTargetDirectory"/>.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="baseTargetDirectory"></param>
        /// <param name="appendMode">Either add new targets to existing ones, or resets them.</param>
        void SetTargets(PathList<IOeFileToBuild> paths, string baseTargetDirectory, bool appendMode = false);

    }
}