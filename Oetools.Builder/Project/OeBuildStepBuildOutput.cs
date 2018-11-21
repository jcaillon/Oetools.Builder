#region header
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
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// A list of steps/tasks that should affect the files in your project output directory.
    /// </summary>
    /// <remarks>
    /// Tasks are executed sequentially in the given order.
    /// These tasks should be used to "post-process" the files built from your source directory into the output directory.
    /// For instance, it can be used to build a release zip file containing all the .pl and other configuration files of your release.
    /// A listing of the files in the output directory is made at each step. Which means it would not be efficient to create 10 steps of 1 task each if those files will not change between steps.
    /// </remarks>
    [Serializable]
    public class OeBuildStepBuildOutput : OeBuildStepFree {
        
        /// <inheritdoc cref="AOeBuildStep.AreTasksBuiltFromIncludeList"/>
        protected override bool AreTasksBuiltFromIncludeList() => false;
        
    }
    
}