// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutor.cs) is part of Oetools.Builder.
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;

namespace Oetools.Builder {
    
    public class TaskExecutor {

        protected List<OeTask> Tasks { get; }
        
        protected ILogger Log { get; set; }

        public TaskExecutor(List<OeTask> tasks) {
            Tasks = tasks;
        }

        public virtual void Execute() {
            foreach (var task in Tasks) {
                Log.Info($"Executing task {task}");
                ExecuteTask(task);
            }
        }

        protected virtual void ExecuteTask(OeTask task) {
            if (!(task is ITaskExecute taskExecute)) {
                throw new TaskExecutorException($"Invalid task type : {task}");
            }
            taskExecute.Execute();
        }
    }
    
    public class TaskExecutorOnFile : TaskExecutor {

        public TaskExecutorOnFile(List<OeTask> tasks) : base(tasks) { }
        
        protected override void ExecuteTask(OeTask task) {
            if (!(task is ITaskExecuteOnFile taskOnFile)) {
                base.ExecuteTask(task);
                return;
            }

            List<OeFile> taskFiles = new List<OeFile>();
            if (File.Exists(taskOnFile.GetOriginalIncludeString())) {
                taskFiles.Add(new OeFile { SourcePath = taskOnFile.GetOriginalIncludeString() });
            } else {
                var directoryToList = Utils.GetLongestValidDirectory(taskOnFile.GetOriginalIncludeString());
                Log.Info($"Listing directory : {directoryToList}");
                taskFiles = Utils.EnumerateAllFiles(directoryToList, SearchOption.AllDirectories, new List<string> { taskOnFile.GetExcludeRegex() })
                    .Select(f => new OeFile { SourcePath = f })
                    .ToList();
            }
            
        }

    }
    
    public class TaskExecutorOnFileList : TaskExecutorOnFile {

        public TaskExecutorOnFileList(List<OeTask> tasks) : base(tasks) { }
        
        protected override void ExecuteTask(OeTask task) {
            if (!(task is ITaskExecuteOnFile taskOnFile)) {
                base.ExecuteTask(task);
                return;
            }
            
        }

    }
}