#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeTaskFilter.cs) is part of Oetools.Builder.
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
using Oetools.Builder.History;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// A task that allows to select files or directories with include/exclude patterns.
    /// </summary>
    public interface IOeTaskFilter : IOeTask {

        /// <summary>
        /// Returns a list of include strings as regular expressions.
        /// </summary>
        /// <returns></returns>
        List<string> GetRegexIncludeStrings();

        /// <summary>
        /// Returns a list of exclude strings as regular expressions.
        /// </summary>
        /// <returns></returns>
        List<string> GetRegexExcludeStrings();

        /// <summary>
        /// Returns the raw list of include strings.
        /// </summary>
        /// <returns></returns>
        List<string> GetIncludeStrings();

        /// <summary>
        /// Returns the raw list of exclude strings.
        /// </summary>
        /// <returns></returns>
        List<string> GetExcludeStrings();

        /// <summary>
        /// Given the inclusion and exclusion patterns, filter the input list of files to only keep files that apply to this task.
        /// </summary>
        /// <param name="originalListOfPaths"></param>
        /// <returns></returns>
        PathList<IOeFile> FilterFiles(PathList<IOeFile> originalListOfPaths);

        /// <summary>
        /// Given the inclusion and exclusion patterns, filter the input list of files to only keep files that apply to this task.
        /// </summary>
        /// <param name="originalListOfPaths"></param>
        /// <returns></returns>
        PathList<IOeDirectory> FilterDirectories(PathList<IOeDirectory> originalListOfPaths);

        /// <summary>
        /// Returns true if the given file passes this filter.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool IsPathPassingFilter(string path);

        /// <summary>
        /// Returns true of the given path is included with this filter.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool IsPathIncluded(string path);

        /// <summary>
        /// Returns true of the given path is excluded with this filter.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool IsPathExcluded(string path);

        /// <summary>
        /// Allows to define another filter on the file extension; only the files with theses extensions will be allowed.
        /// </summary>
        /// <param name="fileExtensionFiler"></param>
        void SetFileExtensionFilter(string fileExtensionFiler);
    }
}
