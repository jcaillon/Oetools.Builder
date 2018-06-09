using Oetools.Packager.Core.Config;

namespace Oetools.Packager.Core.Execution {
    internal class ProExecutionProDesktop : ProExecution {
        public override ExecutionType ExecutionType { get { return ExecutionType.ProDesktop; } }
        public ProExecutionProDesktop(IEnvExecution env) : base(env) { }
    }
}