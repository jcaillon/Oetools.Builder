#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeBuildStepCompile.cs) is part of Oetools.Builder.
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
    /// A list of tasks that will build the files in your project source directory.
    /// This type of step should be used for all the tasks that are affecting files in your source directory.
    /// </summary>
    /// <example>
    /// For this type of step, the openedge compilation is sped up by compiling all the files required at the beginning of the step.
    /// This is the main tasks list, where openedge files should be compiled.
    /// The history of files built here can be saved to enable an incremental build.
    /// A listing of the files in the source directory is done at the beginning of this step. Which means it would not be efficient to create 10 steps of 1 task each if those files are not changing between steps.
    /// </example>
    [Serializable]
    public class OeBuildStepBuildSource : AOeBuildStep {
        
        /// <inheritdoc cref="AOeBuildStep.AreTasksBuiltFromIncludeList"/>
        protected override bool AreTasksBuiltFromIncludeList() => false;
                
        /// <summary>
        /// A list of tasks to build your source files.
        /// </summary>
        [XmlArray("Tasks")]
        [XmlArrayItem("Copy", typeof(OeTaskFileCopy))]
        [XmlArrayItem("Prolib", typeof(OeTaskFileArchiverArchiveProlib))]
        [XmlArrayItem("Cab", typeof(OeTaskFileArchiverArchiveCab))]
        [XmlArrayItem("Compile", typeof(OeTaskFileCompile))]
        [XmlArrayItem("CompileInProlib", typeof(OeTaskFileArchiverArchiveProlibCompile))]
        [XmlArrayItem("CompileInCab", typeof(OeTaskFileArchiverArchiveCabCompile))]
        [XmlArrayItem("ReflectDeletedSourceFile", typeof(OeTaskReflectDeletedSourceFile))]
        [XmlArrayItem("ReflectDeletedTargets", typeof(OeTaskReflectDeletedTargets))]
        public override List<AOeTask> Tasks { get; set; }
    }
}