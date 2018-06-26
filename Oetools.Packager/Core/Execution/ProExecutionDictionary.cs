using Oetools.Packager.Core.Config;

namespace Oetools.Packager.Core.Execution {
    internal class ProExecutionDictionary : ProExecution {
        public override ExecutionType ExecutionType { get { return ExecutionType.Dictionary; } }
        public ProExecutionDictionary(IEnvExecution env) : base(env) { }
    }
}