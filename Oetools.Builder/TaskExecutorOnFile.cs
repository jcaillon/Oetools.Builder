using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;

namespace Oetools.Builder {
    
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
}