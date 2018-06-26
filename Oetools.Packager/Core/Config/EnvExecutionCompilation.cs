using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Archive.Compression;
using Oetools.Utilities.Lib;

namespace Oetools.Packager.Core.Config {
    
    public class EnvExecutionCompilation : EnvExecution, IEnvExecutionCompilation {

        public EnvExecutionCompilation() {
            ArchivesCompressionLevel = CompressionLvl.Max;
        }

        /// <summary>
        /// Base target directory for the compilation
        /// </summary>
        public string TargetDirectory { get; set; }
    
        /// <summary>
        /// Source directory
        /// </summary>
        public string SourceDirectory { get; set; }

        /// <summary>
        /// The deployer for this environment (can either be a new one, or a copy of this proenv is, itself, a copy)
        /// </summary>
        public Deployer Deployer {
            get { return _deployer ?? (_deployer = new Deployer(DeploymentRules.GetRules(FileDeploymentRules, out _ruleErrors), this)); }
        }

        private Deployer _deployer;
        
        private List<Tuple<int, string>> _ruleErrors;

        /// <summary>
        /// Path to the deployment rules
        /// </summary>
        public string FileDeploymentRules { get; set; }

        public bool CompileLocally { get; set; }
        public bool CompileWithDebugList { get; set; }
        public bool CompileWithXref { get; set; }
        public bool CompileWithListing { get; set; }
        public bool CompileUseXmlXref { get; set; }

        
        public CompressionLvl ArchivesCompressionLevel { get; set; }

        /// <summary>
        /// Returns the path to prolib (or null if not found in the dlc folder)
        /// </summary>
        public string ProlibPath {
            get {
                string exeName = Utils.IsRuntimeWindowsPlatform ? "prolib.exe" : "prolib";
                var outputPath = Path.Combine(DlcPath, "bin", exeName);
                return File.Exists(outputPath) ? outputPath : null;
            }
        }

        /// <summary>
        /// True if the progress files that do not have an associated rule should be compiled to 
        /// the target directory directly
        /// </summary>
        public bool CompileUnmatchedProgressFiles { get; set; }

        /// <summary>
        /// Force the usage of a temporary folder to compile the .r code files
        /// </summary>
        public bool CompileForceUseOfTemp { get; set; }

        /// <summary>
        /// Finding files in directories is actually a task that can take a long time,
        /// if we get a match, we save it here so the next time we look for the file,
        /// we already know its full path
        /// </summary>
        private Dictionary<string, string> _savedFoundFiles = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        /// Tries to find the specified file in the current propath
        /// returns null if nothing is found, otherwise returns the fullpath of the file
        /// </summary>
        public string FindFirstFileInPropath(string fileName) {
            if (_savedFoundFiles.ContainsKey(fileName)) {
                return _savedFoundFiles[fileName];
            }

            try {
                foreach (var item in ProPathList) {
                    var curPath = Path.Combine(item, fileName);
                    if (File.Exists(curPath)) {
                        _savedFoundFiles.Add(fileName, curPath);
                        return curPath;
                    }
                }
            } catch (Exception) {
                // The pats in invalid, well we don't really care
            }
            return null;
        }


    }
}
