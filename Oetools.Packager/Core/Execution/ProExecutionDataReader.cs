using Oetools.Packager.Core.Config;

namespace Oetools.Packager.Core.Execution {
    internal class ProExecutionDataReader : ProExecutionDataDigger {

        public override ExecutionType ExecutionType { get { return ExecutionType.DataReader; } }

        public ProExecutionDataReader(IEnvExecution env, string dataDiggerFolder) : base(env, dataDiggerFolder) { }
    }
}