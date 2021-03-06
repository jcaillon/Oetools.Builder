﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeBuildStepClassic.cs) is part of Oetools.Builder.
// 
// Oetools.Builder is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// A list of tasks that should affect the files in your project output directory.
    /// </summary>
    /// <example>
    /// These tasks can be used to 'post-process' the files built into the output directory.
    /// A listing of the files in the output directory is done at the beginning of this step. Which means it would not be efficient to create 10 steps of 1 task each if those files are not changing between steps.
    /// 
    /// Suggested usage:
    ///   - These tasks can be used to modify a configuration file copied from the source directory to the output directory.
    ///   - These tasks can be used to build a release zip file containing all the .pl of your release.
    /// </example>
    [Serializable]
    public class OeBuildStepBuildOutput : OeBuildStepFree {
        
        /// <inheritdoc cref="AOeBuildStep.AreTasksBuiltFromIncludeList"/>
        protected override bool AreTasksBuiltFromIncludeList() => false;
        
    }
    
}