#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (BuildStepExecutor.cs) is part of Oetools.Builder.
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
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder {
    
    public class BuildStepExecutor {
 
        internal string Name { get; set; }

        /// <summary>
        /// The tasks to execute for this step.
        /// </summary>
        public List<IOeTask> Tasks { get; set; }

        /// <summary>
        /// The logger.
        /// </summary>
        internal ILogger Log { get; set; }

        /// <summary>
        /// The build properties.
        /// </summary>
        internal OeProperties Properties { get; set; }

        /// <summary>
        /// The base target directory (if any) for this step.
        /// </summary>
        protected virtual string BaseTargetDirectory => null;

        /// <summary>
        /// Cancel token.
        /// </summary>
        internal CancellationToken? CancelToken { get; set; }
        
        /// <summary>
        /// Total number of tasks already executed.
        /// </summary>
        internal int NumberOfTasksDone { get; private set; }

        /// <summary>
        /// Event published when a task starts.
        /// </summary>
        internal event EventHandler<StepExecutorProgressEventArgs> OnTaskStart;

        /// <summary>
        /// The list of task exceptions for this step.
        /// </summary>
        internal List<TaskExecutionException> TaskExecutionExceptions;
        
        protected bool StopBuildOnTaskWarning => Properties?.BuildOptions?.StopBuildOnTaskWarning ?? OeBuildOptions.GetDefaultStopBuildOnTaskWarning();
        
        protected bool StopBuildOnTaskError => Properties?.BuildOptions?.StopBuildOnTaskError ?? OeBuildOptions.GetDefaultStopBuildOnTaskError();

        protected bool TestMode => Properties?.BuildOptions?.TestMode ?? OeBuildOptions.GetDefaultTestMode();

        /// <summary>
        /// Setup the tasks and start their execution
        /// </summary>
        /// <exception cref="TaskExecutorException"></exception>
        internal void Execute() {
            if (Tasks == null) {
                return;
            }
            TaskExecutionExceptions = null;
            NumberOfTasksDone = 0;
            try {
                Log?.Debug("Injecting task properties.");
                foreach (var task in Tasks) {
                    InjectPropertiesInTask(task);
                }
                Log?.Debug("Set the paths to build for each task.");
                foreach (var task in Tasks) {
                    switch (task) {
                        case IOeTaskFile taskFile: {
                            taskFile.SetFilesToProcess(GetFilesToBuildForSingleTask(taskFile));
                            break;
                        }
                        case IOeTaskDirectory taskDirectory: {
                            taskDirectory.SetDirectoriesToProcess(GetDirectoriesToBuildForSingleTask(taskDirectory));
                            break;
                        }
                    }
                }
                ExecuteInternal();
            } catch (OperationCanceledException) {
                throw;
            } catch (TaskExecutorException) {
                throw;
            } catch (Exception e) {
                throw new TaskExecutorException(this, e.Message, e);
            }
        }

        /// <summary>
        /// Executes all the tasks
        /// </summary>
        /// <exception cref="TaskExecutorException"></exception>
        protected virtual void ExecuteInternal() {
            foreach (var task in Tasks) {
                CancelToken?.ThrowIfCancellationRequested();
                try {
                    task.PublishWarning += TaskOnPublishException;
                    OnTaskStart?.Invoke(this, new StepExecutorProgressEventArgs(NumberOfTasksDone, Tasks.Count, task.ToString()));
                    task.Execute();
                } catch (OperationCanceledException) {
                    throw;
                } catch (TaskExecutionException e) {
                    SaveTaskExecutionException(e);
                    if (StopBuildOnTaskError) {
                        throw new TaskExecutorException(this, e.Message, e);
                    }
                } catch (Exception e) {
                    throw new TaskExecutorException(this, $"Unexpected exception for {task}: {e.Message}", e);
                } finally {
                    task.PublishWarning -= TaskOnPublishException;
                }
                NumberOfTasksDone++;
            }
        }

        /// <summary>
        /// Prepares a task, injecting properties if needed, depending on which interface it implements
        /// </summary>
        /// <param name="task"></param>
        protected virtual void InjectPropertiesInTask(IOeTask task) {
            task.SetLog(Log);
            task.SetTestMode(TestMode);
            task.SetCancelToken(CancelToken);
            if (task is IOeTaskCompile taskCompile) {
                taskCompile.SetFileExtensionFilter(Properties?.CompilationOptions?.CompilableFileExtensionPattern ?? OeCompilationOptions.GetDefaultCompilableFileExtensionPattern());
            }
            if (task is IOeTaskNeedingProperties taskNeedingProperties) {
                taskNeedingProperties.SetProperties(Properties);
            }
            if (task is IOeTaskFileToBuild taskFileToBuild) {
                taskFileToBuild.SetTargetBaseDirectory(BaseTargetDirectory);
            }
        }

        /// <summary>
        /// Should return all the files that need to be built by the given <paramref name="task" />.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        protected virtual PathList<IOeFile> GetFilesToBuildForSingleTask(IOeTaskFile task) {
            return task.GetFilesToProcessFromIncludes();
        }

        /// <summary>
        /// Should return all the directories that need to be built by the given <paramref name="task" />.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        protected virtual PathList<IOeDirectory> GetDirectoriesToBuildForSingleTask(IOeTaskDirectory task) {
            return task.GetDirectoriesToProcessFromIncludes();
        }
        
        private void TaskOnPublishException(object sender, TaskWarningEventArgs e) {
            SaveTaskExecutionException(e.Exception);
            if (StopBuildOnTaskWarning) {
                throw e.Exception;
            }
        }

        private void SaveTaskExecutionException(TaskExecutionException exception) {
            if (TaskExecutionExceptions == null) {
                TaskExecutionExceptions = new List<TaskExecutionException>();
            }
            TaskExecutionExceptions.Add(exception);
            var publishedException = new TaskExecutorException(this, exception.Message, exception);
            Log?.Debug($"Task exception: {publishedException.Message}", publishedException);
        }

        public override string ToString() => Name ?? "Unnamed step executor.";
    }
}