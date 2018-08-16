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