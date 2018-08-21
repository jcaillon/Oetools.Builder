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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
    
    public class TaskExecutor {
        
        internal string Name { get; set; }
        
        private IEnumerable<IOeTask> _tasks;

        public IEnumerable<IOeTask> Tasks {
            get => _tasks;
            set {
                _tasks = value;
                if (_tasks != null) {
                    foreach (var task in _tasks) {
                        InjectPropertiesInTask(task);
                    }
                }
            }
        }

        public ILogger Log { get; set; }

        public UoeExecutionEnv Env { get; set; }
        
        public OeProjectProperties ProjectProperties { get; set; }

        protected virtual string BaseTargetDirectory => null;

        public CancellationTokenSource CancelSource { get; set; }
        
        public bool ThrowIfWarning => ProjectProperties?.TreatWarningsAsErrors ?? OeProjectProperties.GetDefaultTreatWarningsAsErrors();
        
        /// <summary>
        /// Executes all the tasks
        /// </summary>
        /// <exception cref="TaskExecutorException"></exception>
        public virtual void Execute() {
            if (Tasks == null) {
                return;
            }
            foreach (var task in Tasks) {
                Log?.Info($"Executing task {task}");
                CancelSource?.Token.ThrowIfCancellationRequested();
                try {
                    task.PublishException += TaskOnPublishException;
                    ExecuteTask(task);
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new TaskExecutorException(this, $"Unexpected exception when executing {task.ToString().PrettyQuote()} : {e.Message}", e);
                } finally {
                    task.PublishException -= TaskOnPublishException;
                }
            }
        }

        /// <summary>
        /// Executes a single task
        /// </summary>
        /// <param name="task"></param>
        /// <exception cref="TaskExecutorException"></exception>
        protected virtual void ExecuteTask(IOeTask task) {
            switch (task) {
                case IOeTaskFile taskOnFiles:
                    taskOnFiles.ExecuteForFiles(GetFilesReadyForTaskExecution(taskOnFiles, GetTaskFiles(taskOnFiles)));
                    break;
                default:
                    task.Execute();
                    break;
            }
        }

        /// <summary>
        /// Prepares a task, injecting properties if needed, depending on which interface it implements
        /// </summary>
        /// <param name="task"></param>
        protected virtual void InjectPropertiesInTask(IOeTask task) {
            task.SetLog(Log);
            task.SetCancelSource(CancelSource);
            if (task is IOeTaskCompile taskCompile) {
                taskCompile.SetFileExtensionFilter(ProjectProperties?.CompilationOptions?.CompilableFilePattern ?? OeCompilationOptions.GetDefaultCompilableFilePattern());
            }
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
                    file.TargetsFiles = taskWithTargetFiles.GetFileTargets(file.SourcePathForTaskExecution, BaseTargetDirectory);
                }
            }
            if (task is IOeTaskFileTargetArchive taskWithTargetArchives) {
                foreach (var file in initialFiles) {
                    file.TargetsArchives = taskWithTargetArchives.GetFileTargets(file.SourcePathForTaskExecution, BaseTargetDirectory);
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
            return task.GetIncludedFiles();
        }
        
        private void TaskOnPublishException(object sender, TaskExceptionEventArgs e) {
            // if error, cancel the whole execution
            if (!e.IsWarning || ThrowIfWarning) {
                CancelSource.Cancel();
            }
        }

        public override string ToString() => $"Task executor{(string.IsNullOrEmpty(Name) ? "" : $" {Name}")}";
    }
}