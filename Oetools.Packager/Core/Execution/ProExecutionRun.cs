using System.IO;
using System.Linq;

namespace Oetools.Packager.Core.Execution {

    internal class ProExecutionRun : ProExecutionHandleCompilation {

        public override ExecutionType ExecutionType { get { return ExecutionType.Run; } }

        protected override void SetExecutionInfo() {
            base.SetExecutionInfo();

            _processStartDir = Path.GetDirectoryName(Files.First().SourcePath) ?? _localTempDir;
        }

    }
}