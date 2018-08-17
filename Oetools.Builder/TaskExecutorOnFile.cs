using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
    
    public class TaskExecutorOnFile : TaskExecutor {
        
        public EnvExecution Env { get; private set; }

        public OeCompilationOptions CompilationOptions { get; }
        
        public OeProjectProperties ProjectProperties { get; }

        public TaskExecutorOnFile(List<OeTask> tasks) : base(tasks) { }
        
        /// <summary>
        /// List of unique existing files that will be treated by the current list of <see cref="TaskExecutor.Tasks"/>
        /// </summary>
        public List<OeFile> TaskFiles { get; protected set; }

        public override void Execute() {
            
            base.Execute();
        }

        protected override void ExecuteTask(OeTask task) {
            if (!(task is ITaskExecuteOnFile taskOnFile)) {
                base.ExecuteTask(task);
                return;
            }

            TaskFiles = TaskFiles != null ? GetFilteredTaskFiles(taskOnFile) : GetTaskFiles(taskOnFile);

            ExecuteTaskOnFiles(taskOnFile, TaskFiles);
        }
        
        protected virtual void ExecuteTaskOnFiles(ITaskExecuteOnFile task, List<OeFile> files) {
            
        }
        
        /// <summary>
        /// Filter the task files list given the task inclusion and exclusion filters
        /// </summary>
        private List<OeFile> GetFilteredTaskFiles(ITaskExecuteOnFile task) {
            return TaskFiles
                .Where(f => task.IsFileExcluded(f) && !task.IsFileExcluded(f))
                .ToList();
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
                    // the include directly designate a file
                    var newFile = new OeFile { SourcePath = Path.GetFullPath(originalIncludeStrings[i]) };
                    if (!task.IsFileExcluded(newFile)) {
                        output.Add(newFile);
                    }
                } else {
                    // the include is a wildcard path, we try to get the "root" folder to list to get all the files
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
}