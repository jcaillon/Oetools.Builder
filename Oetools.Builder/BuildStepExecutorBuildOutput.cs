using System.IO;
using Oetools.Builder.Utilities;

namespace Oetools.Builder {
    public class BuildStepExecutorBuildOutput : BuildStepExecutorWithFileList {

        public override void Configure() {
            if (Directory.Exists(Properties.BuildOptions.OutputDirectoryPath)) {
                var sourceLister = new SourceFilesLister(Properties.BuildOptions.OutputDirectoryPath, CancelSource) {
                    Log = Log
                };
                TaskFiles = sourceLister.GetFileList();
            }
        }
    }
}