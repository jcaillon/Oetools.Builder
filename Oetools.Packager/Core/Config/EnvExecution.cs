using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Oetools.Packager.Resources.Openedge;

namespace Oetools.Packager.Core.Config {
    
    public class EnvExecution : IEnvExecution {
        
        public string ConnectionString { get; set; }

        public string DatabaseAliasList { get; set; }

        public string IniPath { get; set; }

        public List<string> ProPathList { get; set; }

        /// <summary>
        /// Returns the path to the progress executable (or null if it was not found)
        /// </summary>
        public string ProExePath {
            get {
                string outputPath;
#if WINDOWSONLYBUILD
                bool isWindowPlateform = true;
#else
                bool isWindowPlateform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
                if (isWindowPlateform) {
                    outputPath = Path.Combine(DlcPath, "bin", "prowin32.exe");
                    if (!File.Exists(outputPath)) {
                        outputPath = Path.Combine(DlcPath, "bin", "prowin.exe");
                    }
                    if (!File.Exists(outputPath)) {
                        outputPath = Path.Combine(DlcPath, "bin", "_progres.exe");
                    }
                } else {
                    outputPath = Path.Combine(DlcPath, "bin", "_progres");
                }
                return File.Exists(outputPath) ? outputPath : null;
            }
        }

        public bool UseCharacterModeOfProgress { get; set; }

        public Version ProVersion {
            get {
                var versionFilePath = Path.Combine(DlcPath, "version");
                if (File.Exists(versionFilePath)) {
                    var matches = new Regex(@"(\d+)\.(\d+)(?:\.(\d+)|([A-Za-z](\d+)))").Matches(File.ReadAllText(versionFilePath));
                    if (matches.Count == 1) {
                        return new Version(int.Parse(matches[0].Groups[1].Value), int.Parse(matches[0].Groups[2].Value), int.Parse(matches[0].Groups[3].Success ? matches[0].Groups[3].Value : matches[0].Groups[5].Value));
                    }
                }
                return new Version();
            }
        }

        public string DlcPath { get; set; }

        public string CmdLineParameters { get; set; }
        
        public string PreExecutionProgramPath { get; set; }
        
        public string PostExecutionProgramPath { get; set; }

        public bool NeverUseProwinInBatchMode { get; set; }

        public bool CanProwinUseNoSplash => ProVersion.CompareTo(new Version(11, 6, 0)) >= 0;

        public string FolderTemp { get; set; }
        
        public string ProgramProgressRun => OpenedgeResources.GetOpenedgeAsStringFromResources(@"ProgressRun.p");
        
        public string ProgramDeploymentHook => OpenedgeResources.GetOpenedgeAsStringFromResources(@"DeploymentHook.p");

        public string ProgramStartProlint => OpenedgeResources.GetOpenedgeAsStringFromResources(@"StartProlint.p");
    }
}