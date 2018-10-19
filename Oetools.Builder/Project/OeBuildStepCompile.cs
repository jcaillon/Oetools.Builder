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
    
    [Serializable]
    public class OeBuildStepCompile : OeBuildStep {
                
        [XmlArray("Tasks")]
        [XmlArrayItem("Execute", typeof(OeTaskExec))]
        [XmlArrayItem("Compile", typeof(OeTaskFileTargetFileCompile))]
        [XmlArrayItem("CompileInProlib", typeof(OeTaskFileTargetArchiveProlibCompile))]
        [XmlArrayItem("CompileInZip", typeof(OeTaskFileTargetArchiveZipCompile))]
        [XmlArrayItem("CompileInCab", typeof(OeTaskFileTargetArchiveCabCompile))]
        [XmlArrayItem("CompileUploadFtp", typeof(OeTaskFileTargetArchiveFtpCompile))]
        [XmlArrayItem("Copy", typeof(OeTaskFileTargetFileCopy))]
        [XmlArrayItem("Prolib", typeof(OeTaskFileTargetArchiveProlib))]
        [XmlArrayItem("Zip", typeof(OeTaskFileTargetArchiveZip))]
        [XmlArrayItem("Cab", typeof(OeTaskFileTargetArchiveCab))]
        [XmlArrayItem("UploadFtp", typeof(OeTaskFileTargetArchiveFtp))]
        public List<OeTask> Tasks { get; set; }

        public override List<OeTask> GetTaskList() => Tasks;
        
        internal override void InitIds() => InitIds(Tasks);
    }
}