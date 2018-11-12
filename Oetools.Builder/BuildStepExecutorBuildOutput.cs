using System.IO;
using Oetools.Builder.History;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder {
    
    public class BuildStepExecutorBuildOutput : BuildStepExecutor {
        
        protected override string BaseTargetDirectory => Properties?.BuildOptions?.OutputDirectoryPath;

        private PathList<IOeFile> _outputFilesCompleteList;
        
        private PathList<IOeDirectory> _outputDirectoriesCompleteList;

        protected override PathList<IOeFile> GetFilesToBuildForSingleTask(IOeTaskFile task) {
            Log?.Debug("Gets the list of files on which to apply this task from the output directory.");
            
            if (_outputFilesCompleteList == null) {
                Log?.Debug($"List all the files in output directory: {BaseTargetDirectory.PrettyQuote()}.");
                if (Directory.Exists(Properties.BuildOptions.OutputDirectoryPath)) {
                    var sourceLister = new PathLister(Properties.BuildOptions.OutputDirectoryPath, CancelToken) {
                        Log = Log
                    };
                    _outputFilesCompleteList = sourceLister.GetFileList();
                }
            }
            
            return task.FilterFiles(_outputFilesCompleteList ?? new PathList<IOeFile>());
        }

        protected override PathList<IOeDirectory> GetDirectoriesToBuildForSingleTask(IOeTaskDirectory task) {
            Log?.Debug("Gets the list of directories on which to apply this task from the output directory.");
            
            if (_outputDirectoriesCompleteList == null) {
                Log?.Debug($"List all the directories in output directory: {BaseTargetDirectory.PrettyQuote()}.");
                if (Directory.Exists(Properties.BuildOptions.OutputDirectoryPath)) {
                    var sourceLister = new PathLister(Properties.BuildOptions.OutputDirectoryPath, CancelToken) {
                        Log = Log
                    };
                    _outputDirectoriesCompleteList = sourceLister.GetDirectoryList();
                }
            }
            
            return task.FilterDirectories(_outputDirectoriesCompleteList ?? new PathList<IOeDirectory>());
        }
    }
}