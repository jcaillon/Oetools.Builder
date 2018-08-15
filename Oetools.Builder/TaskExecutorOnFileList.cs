using System.Collections.Generic;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;

namespace Oetools.Builder {
    
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