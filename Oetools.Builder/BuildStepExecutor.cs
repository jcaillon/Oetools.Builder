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
using System.IO;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge;

namespace Oetools.Builder {
    
    public class BuildStepExecutor {
        
        internal string Name { get; set; }
        
        internal int Id { get; set; }
        
        public List<IOeTask> Tasks { get; set; }

        public ILogger Log { protected get; set; }
        
        public OeProperties Properties { get; set; }

        protected virtual string BaseTargetDirectory => null;

        public CancellationTokenSource CancelSource { protected get; set; }
        
        protected bool ThrowIfWarning => Properties?.BuildOptions?.TreatWarningsAsErrors ?? OeBuildOptions.GetDefaultTreatWarningsAsErrors();
        
        protected bool TestMode => Properties?.BuildOptions?.TestMode ?? OeBuildOptions.GetDefaultTestMode();

        /// <summary>
        /// Executes all the tasks
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
                ExecuteInternal();
            } catch (OperationCanceledException) {
                throw;
            } catch (TaskExecutorException) {
                throw;
            } catch (Exception e) {
                throw new TaskExecutorException(this, e.Message, e);
            }
        }

        public virtual void Configure() {
            
        }

        protected virtual void ExecuteInternal() {
            foreach (var task in Tasks) {
                CancelSource?.Token.ThrowIfCancellationRequested();
                try {
                    task.PublishWarning += TaskOnPublishException;
                    Log?.Info($"Starting task {task}");
                    ExecuteTask(task);
                } catch (OperationCanceledException) {
                    throw;
                } catch (TaskExecutionException e) {
                    throw new TaskExecutorException(this, e.Message, e);
                } catch (Exception e) {
                    throw new TaskExecutorException(this, $"Unexpected exception : {task} : {e.Message}", e);
                } finally {
                    task.PublishWarning -= TaskOnPublishException;
                }
            }
        }

        /// <summary>
        /// Executes a single task
        /// </summary>
        /// <param name="task"></param>
        /// <exception cref="TaskExecutionException"></exception>
        private void ExecuteTask(IOeTask task) {
            switch (task) {
                case IOeTaskFile taskOnFiles:
                    taskOnFiles.ExecuteForFiles(GetFilesReadyForTaskExecution(taskOnFiles, GetTaskFiles(taskOnFiles) ?? new FileList<OeFile>()));
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
            task.SetTestMode(TestMode);
            task.SetCancelSource(CancelSource);
            if (task is IOeTaskCompile taskCompile) {
                taskCompile.SetFileExtensionFilter(Properties?.CompilationOptions?.CompilableFileExtensionPattern ?? OeCompilationOptions.GetDefaultCompilableFileExtensionPattern());
                taskCompile.SetProperties(Properties);
            }
        }

        /// <summary>
        /// Returns the list of files to build for the given task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="initialFiles"></param>
        /// <returns></returns>
        protected virtual FileList<OeFile> GetFilesReadyForTaskExecution(IOeTaskFile task, FileList<OeFile> initialFiles) {
            SetFilesTargets(task, initialFiles, BaseTargetDirectory);
            return initialFiles;
        }

        internal static void SetFilesTargets(IOeTask task, FileList<OeFile> initialFiles, string baseTargetDirectory, bool appendMode = false) {
            var taskIsCompileTask = task is IOeTaskCompile;
            switch (task) {
                case IOeTaskFileTargetFile taskWithTargetFiles:
                    foreach (var file in initialFiles) {
                        var newTargets = taskWithTargetFiles.GetFileTargets(file.FilePath, baseTargetDirectory);
                        
                        // change the targets extension to .r for compiled files
                        if (taskIsCompileTask && newTargets != null) {
                            foreach (var targetFile in newTargets) {
                                targetFile.TargetFilePath = Path.ChangeExtension(targetFile.TargetFilePath, UoeConstants.ExtR);
                            }
                        }
                        
                        if (appendMode && file.TargetsFiles != null) {
                            if (newTargets != null) {
                                file.TargetsFiles.AddRange(newTargets);
                            }
                        } else {
                            file.TargetsFiles = newTargets;
                        }
                    }
                    break;
                case IOeTaskFileTargetArchive taskWithTargetArchives:
                    foreach (var file in initialFiles) {
                        var newTargets = taskWithTargetArchives.GetFileTargets(file.FilePath, baseTargetDirectory);
                        
                        // change the targets extension to .r for compiled files
                        if (taskIsCompileTask && newTargets != null) {
                            foreach (var targetArchive in newTargets) {
                                targetArchive.RelativeTargetFilePath = Path.ChangeExtension(targetArchive.RelativeTargetFilePath, UoeConstants.ExtR);
                            }
                        }
                        
                        if (appendMode && file.TargetsArchives != null) {
                            if (newTargets != null) {
                                file.TargetsArchives.AddRange(newTargets);
                            }
                        } else {
                            file.TargetsArchives = newTargets;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Given inclusion and exclusion, returns a list of initialFiles on which to apply the given task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        protected virtual FileList<OeFile> GetTaskFiles(IOeTaskFile task) {
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