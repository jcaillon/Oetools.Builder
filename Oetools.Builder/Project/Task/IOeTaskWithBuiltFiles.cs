#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeTaskFileBuilder.cs) is part of Oetools.Builder.
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
using Oetools.Builder.History;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// A task that can build files. A file is "built" when its source path is located in the source directory.
    /// The <see cref="GetBuiltFiles"/> method will return the files built that should be saved in the build history.
    /// </summary>
    public interface IOeTaskWithBuiltFiles {

        /// <summary>
        /// Returns the list of files built by this task.
        /// </summary>
        /// <returns></returns>
        PathList<IOeFileBuilt> GetBuiltFiles();

    }
}
