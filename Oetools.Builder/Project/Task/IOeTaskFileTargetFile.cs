#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeTaskFileTargetFile.cs) is part of Oetools.Builder.
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

namespace Oetools.Builder.Project.Task {
    
    public interface IOeTaskFileTargetFile : IOeTaskFileTarget {

        /// <summary>
        /// Returns a list of target file path for the corresponding source <paramref name="filePath" />,
        /// relative path are turned into absolute path preprending <paramref name="baseTargetDirectory" />
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="baseTargetDirectory"></param>
        /// <returns></returns>
        List<OeTargetFile> GetTargetsFiles(string filePath, string baseTargetDirectory = null);
        
        /// <summary>
        /// Execute the task for a set of files
        /// </summary>
        /// <remarks>
        /// - The task should create/add a list of files that it builds, list that is returned by <see cref="IOeTaskFileBuilder.GetFilesBuilt"/>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed
        /// - This method can publish warnings using <see cref="OeTask.AddExecutionWarning"/>
        /// </remarks>
        /// <param name="files"></param>
        /// <exception cref="TaskExecutionException"></exception>
        void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files);
        
    }
}