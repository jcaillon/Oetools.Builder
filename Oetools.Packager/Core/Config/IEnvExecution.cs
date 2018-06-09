using System.Collections.Generic;

namespace Oetools.Packager.Core.Config {
    public interface IEnvExecution {

        string ConnectionString { get; set; }

        /// <summary>
        /// Format : ALIAS,DATABASE;ALIAS2,DATABASE;...
        /// </summary>
        string DatabaseAliasList { get; set; }

        /// <summary>
        /// Propath (can be null, in that case we automatically add all the folders of the source dir)
        /// </summary>
        string IniPath { get; set; }

        List<string> GetProPathDirList { get; set; }

        /// <summary>
        /// Path to prowin32.exe
        /// </summary>
        string ProwinPath { get; set; }

        string CmdLineParameters { get; set; }
        string PreExecutionProgram { get; set; }
        string PostExecutionProgram { get; set; }
        bool NeverUseProwinInBatchMode { get; set; }
        bool CanProwinUseNoSplash { get; set; }
        string FolderTemp { get; set; }
        byte[] ProgramProgressRun { get; }
    }
}