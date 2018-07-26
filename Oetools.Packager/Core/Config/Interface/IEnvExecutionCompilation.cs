using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Archive.Compression;

namespace Oetools.Packager.Core.Config {
    
    public interface IEnvExecutionCompilation : IEnvExecution {
        
        /// <summary>
        /// Base target directory for the compilation
        /// </summary>
        string TargetDirectory { get; set; }

        /// <summary>
        /// Source directory
        /// </summary>
        string SourceDirectory { get; set; }

        List<DeployRule> CompilationRules { get; set; }
        
        /// <summary>
        /// The deployer for this environment (can either be a new one, or a copy of this proenv is, itself, a copy)
        /// </summary>
        Deployer Deployer { get; }
        
        
        /// <summary>
        /// Path to the deployment rules
        /// </summary>
        string FileDeploymentRules { get; set; }

        bool CompileLocally { get; set; }
        bool CompileWithDebugList { get; set; }
        bool CompileWithXref { get; set; }
        bool CompileWithListing { get; set; }
        bool CompileUseXmlXref { get; set; }
        CompressionLvl ArchivesCompressionLevel { get; set; }

        /// <summary>
        /// Returns the path to prolib (or null if not found in the dlc folder)
        /// </summary>
        string ProlibPath { get; }

        /// <summary>
        /// True if the progress files that do not have an associated rule should be compiled to 
        /// the target directory directly
        /// </summary>
        bool CompileUnmatchedProgressFiles { get; set; }

        /// <summary>
        /// Force the usage of a temporary folder to compile the .r code files
        /// </summary>
        bool CompileForceUseOfTemp { get; set; }

        string ProgramDeploymentHook { get; }
        
        string ProgramStartProlint { get; }

        /// <summary>
        /// Tries to find the specified file in the current propath
        /// returns null if nothing is found, otherwise returns the fullpath of the file
        /// </summary>
        string FindFirstFileInPropath(string fileName);
    }
}