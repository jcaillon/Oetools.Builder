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
using Oetools.Builder.Utilities;
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
        
        public List<OeFile> TaskFiles { get; protected set; }
        
        protected override void ExecuteTask(OeTask task) {
            if (!(task is ITaskExecuteOnFile taskOnFile)) {
                base.ExecuteTask(task);
                return;
            }

            TaskFiles = GetTaskFiles(taskOnFile);

            ExecuteTaskOnFiles(taskOnFile, TaskFiles);
        }
        
        protected virtual void ExecuteTaskOnFiles(ITaskExecuteOnFile task, List<OeFile> files) {
            
        }

        /// <summary>
        /// Given inclusion and exclusion, returns a list of files on which to apply the given task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private List<OeFile> GetTaskFiles(ITaskExecuteOnFile task) {
            var output = new List<OeFile>();
            var originalIncludeStrings = task.GetIncludeOriginalStrings();
            var directoriesToList = new List<string>();
            for (int i = 0; i < originalIncludeStrings.Count; i++) {
                if (File.Exists(originalIncludeStrings[i])) {
                    var newFile = new OeFile { SourcePath = originalIncludeStrings[i] };
                    if (!task.IsFileExcluded(newFile)) {
                        output.Add(newFile);
                    }
                } else {
                    directoriesToList.Add(Utils.GetLongestValidDirectory(originalIncludeStrings[i]));
                }
            }
            if (directoriesToList.Count > 0) {
                Log.Info($"Listing directories : {string.Join(",", directoriesToList)}");
                output.AddRange(Utils.EnumerateAllFiles(directoriesToList, task.GetExcludeRegexStrings())
                    .Select(f => new OeFile { SourcePath = f })
                    .ToList());
            }
            return output;
        }
    }
    
    public class TaskExecutorOnFileList : TaskExecutorOnFile {

        public TaskExecutorOnFileList(List<OeTask> tasks, List<OeFile> taskFiles) : base(tasks) {
            TaskFiles = taskFiles;
        }
        
        protected override void ExecuteTask(OeTask task) {
            if (!(task is ITaskExecuteOnFile taskOnFile)) {
                base.ExecuteTask(task);
                return;
            }

            TaskFiles = GetFilteredTaskFiles(taskOnFile);

            ExecuteTaskOnFiles(taskOnFile, TaskFiles);
        }

        /// <summary>
        /// Filter the task files list given the task inclusion and exclusion filters
        /// </summary>
        private List<OeFile> GetFilteredTaskFiles(ITaskExecuteOnFile task) {
            return TaskFiles
                .Where(f => task.IsFileExcluded(f) && !task.IsFileExcluded(f))
                .ToList();
        }
    }
}