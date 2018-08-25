#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutorWithFileList.cs) is part of Oetools.Builder.
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
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder {
    
    public class TaskExecutorWithFileList : TaskExecutor {
        
        private List<OeFile> _taskFiles;

        /// <summary>
        /// List of unique existing files that will be treated by the current list of <see cref="TaskExecutor.Tasks"/>
        /// </summary>
        public List<OeFile> TaskFiles {
            get => _taskFiles;
            set => _taskFiles = value.ToList(); // copy the list
        }

        protected override string BaseTargetDirectory => Properties?.BuildOptions?.OutputDirectoryPath;

        protected override List<OeFile> GetTaskFiles(IOeTaskFile task) {
            Log?.Debug("Gets the list of files on which to apply this task from input files list");
            return TaskFiles.Where(f => task.IsFilePassingFilter(f.SourceFilePath)).ToList();
        }
       
    }
}