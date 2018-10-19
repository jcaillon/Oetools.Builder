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
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder {
    
    public class BuildStepExecutor {
 
        internal string Name { get; set; }

        internal int Id { get; set; }

        public List<IOeTask> Tasks { get; set; }

        public ILogger Log { protected get; set; }

        public OeProperties Properties { get; set; }

        protected virtual string BaseTargetDirectory => null;

        public CancellationToken? CancelToken { protected get; set; }
        
        public int NumberOfTasksDone { get; private set; }

        public event EventHandler<StepExecutorProgressEventArgs> OnTaskStart;

        protected bool ThrowIfWarning => Properties?.BuildOptions?.TreatWarningsAsErrors ?? OeBuildOptions.GetDefaultTreatWarningsAsErrors();

        protected bool TestMode => Properties?.BuildOptions?.TestMode ?? OeBuildOptions.GetDefaultTestMode();

        /// <summary>
        /// Setup the tasks and start their execution
        /// </summary>
        /// <exception cref="TaskExecutorException"></exception>
        public void Execute() {
            if (Tasks == null) {
                return;
            }
            try {
                Log?.Debug("Injecting task properties");
                foreach (var task in Tasks) {
                    InjectPropertiesInTask(task);
                }
                Log?.Debug("Set the files to build for each task and their targets if needed");
                foreach (var task in Tasks) {
                    if (task is IOeTaskFile taskFile) {
                        var filesToBuildForTask = GetFilesToBuildForSingleTask(taskFile).Select(f => f.GetDeepCopy());
                        taskFile.SetTargetForFiles(filesToBuildForTask, BaseTargetDirectory);
                        taskFile.SetFilesToBuild(filesToBuildForTask);
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
                    OnTaskStart?.Invoke(this, new StepExecutorProgressEventArgs {
                        NumberOfTasksDone = NumberOfTasksDone,
                        CurrentTask = task.ToString()
                    });
                    task.Execute();
                } catch (OperationCanceledException) {
                    throw;
                } catch (TaskExecutionException e) {
                    throw new TaskExecutorException(this, e.Message, e);
                } catch (Exception e) {
                    throw new TaskExecutorException(this, $"Unexpected exception : {task} : {e.Message}", e);
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
        private void InjectPropertiesInTask(IOeTask task) {
            task.SetLog(Log);
            task.SetTestMode(TestMode);
            task.SetCancelToken(CancelToken);
            if (task is IOeTaskCompile taskCompile) {
                taskCompile.SetFileExtensionFilter(Properties?.CompilationOptions?.CompilableFileExtensionPattern ?? OeCompilationOptions.GetDefaultCompilableFileExtensionPattern());
            }
            if (task is IOeTaskNeedingProperties taskNeedingProperties) {
                taskNeedingProperties.SetProperties(Properties);
            }
        }

        /// <summary>
        /// Should return all the files that need to be built by the given <paramref name="task"></paramref>
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        protected virtual PathList<OeFile> GetFilesToBuildForSingleTask(IOeTaskFile task) {
            Log?.Debug("Gets the list of files on which to apply this task from path inclusion");
            return task.GetIncludedFiles();
        }
        
        private void TaskOnPublishException(object sender, TaskWarningEventArgs e) {
            var publishedException = new TaskExecutorException(this, e.Exception.Message, e.Exception);
            Log?.Warn($"Task warning : {publishedException.Message}", publishedException);
            if (ThrowIfWarning) {
                throw e.Exception;
            }
        }

        public override string ToString() => $"{(!string.IsNullOrEmpty(Name) ? $"{Name} ": "")}step {Id}";
    }
}