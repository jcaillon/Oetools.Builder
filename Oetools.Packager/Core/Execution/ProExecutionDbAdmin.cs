using Oetools.Packager.Core.Config;

namespace Oetools.Packager.Core.Execution {
    internal class ProExecutionDbAdmin : ProExecution {
        public override ExecutionType ExecutionType { get { return ExecutionType.DbAdmin; } }
        public ProExecutionDbAdmin(IEnvExecution env) : base(env) { }
    }
}