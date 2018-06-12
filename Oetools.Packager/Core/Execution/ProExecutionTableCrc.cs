using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Oetools.Packager.Core.Config;
using Oetools.Utilities.Lib;

namespace Oetools.Packager.Core.Execution {

    /// <summary>
    ///     Allows to output a file containing the structure of the database
    /// </summary>
    public class ProExecutionTableCrc : ProExecution {

        /// <summary>
        ///     Copy of the pro env to use
        /// </summary>
        public ConfigExecutionDatabase Config { get; private set; }

        public ProExecutionTableCrc(ConfigExecutionDatabase config, IEnvExecution env) : base(env) {
            Config = config;
        }

        #region Methods

        /// <summary>
        ///     Get a list with all the tables + CRC
        /// </summary>
        /// <returns></returns>
        public List<TableCrc> GetTableCrc() {
            var output = new List<TableCrc>();
            Utils.ForEachLine(OutputPath, new byte[0], (i, line) => {
                var split = line.Split('\t');
                if (split.Length == 2)
                    output.Add(new TableCrc {
                        QualifiedTableName = split[0],
                        Crc = split[1]
                    });
            }, Encoding.Default);
            return output;
        }

        #endregion

        #region Properties

        public override ExecutionType ExecutionType {
            get { return ExecutionType.TableCrc; }
        }

        /// <summary>
        /// File to the output path that contains the CRC of each table
        /// </summary>
        public string OutputPath { get; set; }

        #endregion

        #region Override

        protected override void SetExecutionInfo() {
            base.SetExecutionInfo();

            OutputPath = Path.Combine(_localTempDir, "db.extract");
            SetPreprocessedVar("OutputPath", OutputPath.PreProcQuoter());

            var fileToExecute = "db_" + DateTime.Now.ToString("yyMMdd_HHmmssfff") + ".p";
            File.WriteAllText(Path.Combine(_localTempDir, fileToExecute), Config.ProgramDumpTableCrc);
            SetPreprocessedVar("CurrentFilePath", fileToExecute.PreProcQuoter());
            SetPreprocessedVar("DatabaseExtractCandoTblType", Config.DatabaseExtractCandoTblType.Trim().PreProcQuoter());
            SetPreprocessedVar("DatabaseExtractCandoTblName", Config.DatabaseExtractCandoTblName.Trim().PreProcQuoter());
        }

        protected override bool CanUseBatchMode() {
            return true;
        }

        #endregion
    }
}