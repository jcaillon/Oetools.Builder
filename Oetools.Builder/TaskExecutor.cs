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
        
        protected List<OeTask> Tasks { get; }
        
        public ILogger Log { get; set; }

        public EnvExecution Env { get; set; }
        
        public OeProjectProperties ProjectProperties { get; set; }

        public TaskExecutor(List<OeTask> tasks) {
            Tasks = tasks;
        }
        
        public virtual void Execute() {
            foreach (var task in Tasks) {
                Log?.Info($"Executing task {task}");
                ExecuteTask(task);
            }
        }
        
        protected virtual void ExecuteTask(OeTask task) {
            switch (task) {
                case OeTaskOnFile taskOnFile:
                    ExecuteTaskOnFiles(taskOnFile, GetTaskFiles(taskOnFile));
                    break;
                case ITaskExecute taskExecute:
                    taskExecute.Execute();
                    break;
            }
            throw new TaskExecutorException($"Invalid task type : {task}");
        }
                
        /// <summary>
        /// Given inclusion and exclusion, returns a list of files on which to apply the given task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        protected virtual List<OeFile> GetTaskFiles(OeTaskOnFile task) {
            var output = new List<OeFile>();
            foreach (var path in task.GetIncludedPathToList()) {
                if (File.Exists(path)) {
                    if (!task.IsFileExcluded(path)) {
                        output.Add(new OeFile { SourcePath = Path.GetFullPath(path) });
                    }
                } else {
                    Log?.Info($"Listing directory : {path.PrettyQuote()}");
                    output.AddRange(new SourceFilesLister(path) { SourcePathFilter = task }.GetFileList());
                }
            }
            return output;
        }
        
        protected virtual void ExecuteTaskOnFiles(OeTaskOnFile task, List<OeFile> files) {
            task.SetLog(Log);
            foreach (var oeFile in files) {
                task.ExecuteForFile(oeFile);
            }
        }

    }
}