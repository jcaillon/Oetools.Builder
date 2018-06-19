using System.IO;
using System.Text;
using Oetools.Packager.Core.Config;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Packager.Core.Execution {
    public class ProExecutionProVersion : ProExecution {
        private string _outputPath;

        public override ExecutionType ExecutionType {
            get { return ExecutionType.ProVersion; }
        }

        public string ProVersion {
            get { return Utils.ReadAllText(_outputPath, Encoding.Default); }
        }

        public ProExecutionProVersion(EnvExecution proEnv) : base(proEnv) { }

        protected override void SetExecutionInfo() {
            base.SetExecutionInfo();

            _outputPath = Path.Combine(_localTempDir, "pro.version");
            SetPreprocessedVar("OutputPath", _outputPath.PreProcQuoter());
        }

        protected override void AppendProgressParameters(StringBuilder sb) {
            sb.Clear();
            _exeParameters.Append(" -b -p " + _runnerPath.Quoter());
        }

        protected override bool CanUseBatchMode() {
            return true;
        }
    }
}