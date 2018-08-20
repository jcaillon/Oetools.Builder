#region header
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
#endregion
using System.Collections.Generic;
using System.IO;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
    
    public class TaskExecutor {
        
        public IEnumerable<IOeTask> Tasks { get; set; }
        
        public ILogger Log { get; set; }

        public UoeExecutionEnv Env { get; set; }
        
        public OeProjectProperties ProjectProperties { get; set; }

        protected virtual string BaseTargetDirectory => null;
        
        /// <summary>
        /// Executes all the tasks
        /// </summary>
        public virtual void Execute() {
            if (Tasks == null) {
                return;
            }
            foreach (var task in Tasks) {
                Log?.Info($"Executing task {task}");
                ExecuteTask(task);
            }
        }
        
        /// <summary>
        /// Executes a single task
        /// </summary>
        /// <param name="task"></param>
        /// <exception cref="TaskExecutorException"></exception>
        protected virtual void ExecuteTask(IOeTask task) {
            InjectPropertiesInTask(task);
            switch (task) {
                case IOeTaskFile taskOnFiles:
                    taskOnFiles.ExecuteForFiles(GetFilesReadyForTaskExecution(taskOnFiles, GetTaskFiles(taskOnFiles)));
                    break;
                case IOeTaskExecute taskExecute:
                    taskExecute.Execute();
                    break;
            }
            throw new TaskExecutorException($"Invalid task type : {task}");
        }

        /// <summary>
        /// Prepares a task, injecting properties if needed, depending on which interface it implements
        /// </summary>
        /// <param name="task"></param>
        protected virtual void InjectPropertiesInTask(IOeTask task) {
            task.SetLog(Log);
        }

        /// <summary>
        /// Returns the list of files to build for the given task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="initialFiles"></param>
        /// <returns></returns>
        protected virtual IEnumerable<IOeFileToBuildTargetFile> GetFilesReadyForTaskExecution(IOeTaskFile task, List<OeFile> initialFiles) {
            if (task is IOeTaskFileTargetFile taskWithTargetFiles) {
                foreach (var file in initialFiles) {
                    file.TargetsFiles = taskWithTargetFiles.GetFileTargets(file.SourceFilePath, BaseTargetDirectory);
                }
            }
            if (task is IOeTaskFileTargetArchive taskWithTargetArchives) {
                foreach (var file in initialFiles) {
                    file.TargetsArchives = taskWithTargetArchives.GetFileTargets(file.SourceFilePath, BaseTargetDirectory);
                }
            }
            return initialFiles;
        }
        
        /// <summary>
        /// Given inclusion and exclusion, returns a list of initialFiles on which to apply the given task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        protected virtual List<OeFile> GetTaskFiles(IOeTaskFile task) {
            var output = new List<OeFile>();
            foreach (var path in task.GetIncludedPathToList()) {
                if (File.Exists(path)) {
                    if (!task.IsFileExcluded(path)) {
                        output.Add(new OeFile { SourceFilePath = Path.GetFullPath(path) });
                    }
                } else {
                    Log?.Info($"Listing directory : {path.PrettyQuote()}");
                    output.AddRange(new SourceFilesLister(path) { SourcePathFilter = task }.GetFileList());
                }
            }
            return output;
        }
    }
}