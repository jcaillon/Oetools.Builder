using System;
using System.Collections.Generic;

namespace Oetools.Packager.Core.Config {
    public interface IEnvExecution {

        /// <summary>
        /// Connection string to use for the database connection in a CONNECT statement
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        /// Format : ALIAS,DATABASE;ALIAS2,DATABASE;...
        /// </summary>
        string DatabaseAliasList { get; set; }

        /// <summary>
        /// Path to the .ini file (to define FONTS/COLORS mostly, the PROPATH is emptied and replace by <see cref="ProPathList"/>
        /// </summary>
        string IniPath { get; set; }

        /// <summary>
        /// Propath, list of directories/.pl
        /// </summary>
        List<string> ProPathList { get; set; }

        /// <summary>
        /// Returns the path to the progress executable prowin/_progres (or null if it was not found)
        /// </summary>
        string ProExePath { get; }
        
        /// <summary>
        /// True to use the _progres executable instead of the prowin executable (on windows only)
        /// </summary>
        bool UseCharacterModeOfProgress { get; set; }
        
        /// <summary>
        /// Path to the dlc folder (openedge installation folder)
        /// </summary>
        string DlcPath { get; set; }
        
        /// <summary>
        /// Returns the openedge version currently installed
        /// </summary>
        /// <remarks>https://knowledgebase.progress.com/articles/Article/P126</remarks>
        Version ProVersion { get; }

        /// <summary>
        /// Command line parameters to append to the execution of <see cref="ProExePath"/>
        /// </summary>
        string CmdLineParameters { get; set; }
        
        /// <summary>
        /// Path of the .p program that should be executed at the start of an openedge session
        /// </summary>
        string PreExecutionProgramPath { get; set; }
        
        /// <summary>
        /// Path of the .p program that should be executed at the end of an openedge session
        /// </summary>
        string PostExecutionProgramPath { get; set; }
        
        /// <summary>
        /// Force to never use the -b parameter even if we could
        /// </summary>
        bool NeverUseProwinInBatchMode { get; set; }
        
        /// <summary>
        /// Indicates whether or not the -nosplash parameter is available for this version of openedge
        /// </summary>
        bool CanProwinUseNoSplash { get; }
        
        /// <summary>
        /// Temporary folder used when executing openedge
        /// </summary>
        string FolderTemp { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        string ProgramProgressRun { get; }
    }
}