using System.Collections.Generic;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;

namespace Oetools.Builder {
    
    public class TaskExecutorWithFileList : TaskExecutor {
        
        /// <summary>
        /// List of unique existing files that will be treated by the current list of <see cref="TaskExecutor.Tasks"/>
        /// </summary>
        protected List<OeFile> TaskFiles { get; set; }
        
        public TaskExecutorWithFileList(List<OeTask> tasks) : base(tasks) { }

        protected override List<OeFile> GetTaskFiles(OeTaskOnFile task) {
            return TaskFiles.Where(f => task.IsFilePassingFilter(f.SourcePath)).ToList();
        }
        
    }
}