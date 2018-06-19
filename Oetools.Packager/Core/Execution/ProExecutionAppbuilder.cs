using Oetools.Packager.Core.Config;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Packager.Core.Execution {
    internal class ProExecutionAppbuilder : ProExecution {

        public override ExecutionType ExecutionType { get { return ExecutionType.Appbuilder; } }

        public string CurrentFile { get; set; }

        protected override void SetExecutionInfo() {
            base.SetExecutionInfo();
            SetPreprocessedVar("CurrentFilePath", CurrentFile.PreProcQuoter());
        }

        public ProExecutionAppbuilder(IEnvExecution proEnv) : base(proEnv) { }
    }
}