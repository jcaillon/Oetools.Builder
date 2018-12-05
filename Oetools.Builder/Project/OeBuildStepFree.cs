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
    
    /// <inheritdoc cref="OeBuildConfiguration.BuildSteps"/>
    /// <example>
    /// This build step is referred as 'free' because, contrary other type of build step, it does not require to list the files in the source/output directory prior to running the tasks.
    /// Instead, tasks in this step can handle files that are not necessarily located in your source directory (nor in your output directory).
    /// 
    /// Suggested usage:
    ///   - These tasks can be used to "prepare" a build: downloading dependencies/packages or modifying certain source files.
    ///   - These tasks can be used to "deploy" a build: uploading a release zip file to a distant http or ftp server.
    /// </example>
    [Serializable]
    public class OeBuildStepFree : AOeBuildStep {
        
        /// <inheritdoc cref="AOeBuildStep.AreTasksBuiltFromIncludeList"/>
        protected override bool AreTasksBuiltFromIncludeList() => true;
        
        /// <summary>
        /// A list of tasks.
        /// </summary>
        [XmlArray("Tasks")]
        [XmlArrayItem("Execute", typeof(OeTaskExec))]
        [XmlArrayItem("RemoveDirectory", typeof(OeTaskDirectoryDelete))]
        [XmlArrayItem("Delete", typeof(OeTaskFileDelete))]
        [XmlArrayItem("Cab", typeof(OeTaskFileArchiverArchiveCab))]
        public override List<AOeTask> Tasks { get; set; }
    }
}