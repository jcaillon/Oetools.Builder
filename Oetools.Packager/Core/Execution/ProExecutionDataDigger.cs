﻿using System.IO;
using System.Text;
using Oetools.Packager.Core.Config;
using Oetools.Packager.Core.Exceptions;

namespace Oetools.Packager.Core.Execution {
    internal class ProExecutionDataDigger : ProExecution {

        public string DataDiggerFolder { get; private set; }

        public ProExecutionDataDigger(IEnvExecution env, string dataDiggerFolder) : base(env) {
            DataDiggerFolder = dataDiggerFolder;
        }

        public override ExecutionType ExecutionType { get { return ExecutionType.DataDigger; } }

        protected override void SetExecutionInfo() {
            base.SetExecutionInfo();

            if (string.IsNullOrEmpty(DataDiggerFolder) || !Directory.Exists(DataDiggerFolder)) {
                throw new ExecutionParametersException("Could not start datadigger, the installation folder was not found : " + (DataDiggerFolder ?? "No directory specified"));
            }

            // add the datadigger folder to the propath
            _propath = DataDiggerFolder + "," + _propath;
            _processStartDir = DataDiggerFolder;

        }

        protected override void AppendProgressParameters(StringBuilder sb) {
            sb.Clear();
            //sb.Append(" -basekey " + "INI".Quoter());
            sb.Append(" -s 10000 -d dmy -E -rereadnolock -h 255 -Bt 4000 -tmpbsize 8 ");
            sb.Append(" -T " + _localTempDir.Trim('\\').Quoter());
        }

    }
}