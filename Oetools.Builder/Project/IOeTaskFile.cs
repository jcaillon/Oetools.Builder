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
using System.Collections.Generic;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;

namespace Oetools.Builder.Project {
    
    public interface IOeTaskFile : IOeTaskFilter {
        
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Every files passed to that method should either be added to a listed returned by <see cref="GetFilesBuilt"/>
        /// or the task should publish an exception with <see cref="IOeTask.PublishException"/>
        /// </remarks>
        /// <param name="files"></param>
        void ExecuteForFiles(IEnumerable<IOeFileToBuildTargetFile> files);

        List<OeFileBuilt> GetFilesBuilt();

        /// <summary>
        /// Given the inclusion wildcard paths and exclusion patterns, returns a list of files on which to apply this task
        /// </summary>
        /// <returns></returns>
        List<OeFile> GetIncludedFiles();

        /// <summary>
        /// Validates that the task can be applied on files without having a base directory to list; for that,
        /// the task must have an included path (and should not use regex inclusion)
        /// </summary>
        /// <exception cref="TaskExecutionException"></exception>
        void ValidateCanIncludeFiles();
    }
}