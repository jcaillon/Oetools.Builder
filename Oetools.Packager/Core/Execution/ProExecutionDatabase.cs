using System;
using System.IO;
using Oetools.Packager.Core.Config;
using Oetools.Packager.Core.Exceptions;
using Oetools.Utilities.Lib;

namespace Oetools.Packager.Core.Execution {
    /// <summary>
    /// Allows to output a file containing the structure of the database
    /// </summary>
    internal class ProExecutionDatabase : ProExecution {

        /// <summary>
        ///     Copy of the pro env to use
        /// </summary>
        public ConfigExecutionDatabase Config { get; private set; }

        public ProExecutionDatabase(ConfigExecutionDatabase config, IEnvExecution env) : base(env) {
            Config = config;
        }

        #region Properties

        public override ExecutionType ExecutionType { get { return ExecutionType.Database; } }

        /// <summary>
        /// File to the output path that contains the structure of the database
        /// </summary>
        public string OutputPath { get; set; }

        #endregion

        #region Override

        protected override void SetExecutionInfo() {
            base.SetExecutionInfo();

            OutputPath = Path.Combine(_localTempDir, "db.extract");
            var fileToExecute = "db_" + DateTime.Now.ToString("yyMMdd_HHmmssfff") + ".p";

            SetPreprocessedVar("OutputPath", OutputPath.PreProcQuoter());
            SetPreprocessedVar("CurrentFilePath", fileToExecute.PreProcQuoter());

            try {
                File.WriteAllBytes(Path.Combine(_localTempDir, fileToExecute), Config.ProgramDumpDatabase);
            } catch (Exception e) {
                throw new ExecutionParametersException("Couldn't start an execution, couldn't create the dump database program file : " + e.Message, e);
            }
        }
        
        protected override bool CanUseBatchMode() {
            return true;
        }

        #endregion

        
    }
}