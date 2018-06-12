﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ResultDeployment.cs) is part of Oetools.Packager.
// 
// Oetools.Packager is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Packager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Packager. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using Oetools.Packager.Core.Exceptions;

namespace Oetools.Packager.Core.Config {
    
    public class ResultDeployment {
        
        public ReturnCode ReturnCode { get; set; }
        
        public DateTime? DeploymentStartTime { get; set; }
        public DateTime? CompilationStartTime { get; set; }
        
        public TimeSpan? TotalDeploymentTime { get; set; }
        public TimeSpan? TotalCompilationTime { get; set; }

        public int TotalNumberOfProcesses { get; set; }
        
        public List<DeploymentException> HandledExceptions { get; set; }

        /// <summary>
        /// List all the files that were deployed from the source directory
        /// </summary>
        public List<FileDeployed> DeployedFiles { get; set; }
        
        public List<(string filePath, List<(int Line, string ErrorDescription)> rulesErrors)> RulesErrors { get; set; }       
        
        /// <summary>
        /// List of the compilation errors found
        /// </summary>
        public List<FileError> CompilationErrors { get; set; }

        
        public Dictionary<int, List<FileToDeploy>> FilesToDeployPerStep { get; set; }
        
        public int TotalNumberOfFilesToCompile { get; set; }
        
        /// <summary>
        ///     List of all the files that needed to be compiled
        /// </summary>
        public List<FileToCompile> ListFilesToCompile { get; set; }

        /// <summary>
        ///     List of all the files that needed to be deployed after the compilation
        /// </summary>
        public List<FileToDeploy> ListFilesToDeploy { get; set; }
    }
}