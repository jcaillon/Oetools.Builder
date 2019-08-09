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
using DotUtilities;

namespace Oetools.Builder.History {

    /// <summary>
    /// Represents a file that needs to be build and thus which have targets.
    /// </summary>
    public interface IOeFileToBuild : IOeFile {

        /// <summary>
        /// Can be different from <see cref="IPathListItem.Path"/> for instance in the case of a .p, <see cref="PathForTaskExecution"/>
        /// will be set to the path of the .r code to copy instead of the actual source path
        /// </summary>
        string PathForTaskExecution { get; set; }

        /// <summary>
        /// The list of targets to build.
        /// </summary>
        List<AOeTarget> TargetsToBuild { get; set; }
    }
}
