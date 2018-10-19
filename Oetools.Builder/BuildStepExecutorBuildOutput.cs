using System.IO;
using Oetools.Builder.History;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder {
    
    public class BuildStepExecutorBuildOutput : BuildStepExecutor {
        
        protected override string BaseTargetDirectory => Properties?.BuildOptions?.OutputDirectoryPath;

        private PathList<OeFile> _outputDirectoryCompletePathList;

        protected override PathList<OeFile> GetFilesToBuildForSingleTask(IOeTaskFile task) {
            Log?.Debug("Gets the list of files on which to apply this task from the output directory");
            
            if (_outputDirectoryCompletePathList == null) {
                Log?.Debug($"List all the files in output directory {BaseTargetDirectory.PrettyQuote()}");
                if (Directory.Exists(Properties.BuildOptions.OutputDirectoryPath)) {
                    var sourceLister = new PathLister(Properties.BuildOptions.OutputDirectoryPath, CancelToken) {
                        Log = Log
                    };
                    _outputDirectoryCompletePathList = sourceLister.GetFileList();
                }
            }
            
            return task.FilterFiles(_outputDirectoryCompletePathList ?? new PathList<OeFile>());
        }
    }
}