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
        public List<OeFile> TaskFiles { get; set; }
        
        public string SourceDirectory { get; set; }

        public string OutputDirectory { get; set; }
        
        protected override List<OeFile> GetTaskFiles(OeTaskOnFiles task) {
            return TaskFiles.Where(f => task.IsFilePassingFilter(f.SourcePath)).ToList();
        }
        
    }
}