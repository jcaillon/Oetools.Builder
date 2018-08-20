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

namespace Oetools.Builder.Project {
    public interface IOeTaskFilter : IOeTask {
        List<string> GetRegexIncludeStrings();
        List<string> GetRegexExcludeStrings();
        List<string> GetIncludeStrings();
        List<string> GetExcludeStrings();

        /// <summary>
        /// Returns true if the given file passes this filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool IsFilePassingFilter(string filePath);

        /// <summary>
        /// Returns true of the given path is included with this filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool IsFileIncluded(string filePath);

        /// <summary>
        /// Returns true of the given path is excluded with this filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool IsFileExcluded(string filePath);
    }
}