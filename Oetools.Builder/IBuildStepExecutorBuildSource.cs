using System.Collections.Generic;
using Oetools.Builder.History;
using Oetools.Builder.Project.Task;
using Oetools.Utilities.Lib;

namespace Oetools.Builder {
    internal interface IBuildStepExecutorBuildSource {
        FileList<OeFileBuilt> PreviouslyBuiltFiles { get; set; }
        IEnumerable<OeTask> AllTasksOfAllSteps { get; set; }
        bool IsLastBuildStepExecutor { get; set; }
    }
}