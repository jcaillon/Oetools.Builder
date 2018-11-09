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
    [Serializable]
    public class OeBuildStepClassic : OeBuildStep {
                
        [XmlArray("Tasks")]
        [XmlArrayItem("Execute", typeof(OeTaskExec))]
        [XmlArrayItem("RemoveDirectory", typeof(OeTaskDirectoryDelete))]
        [XmlArrayItem("Delete", typeof(OeTaskFileDelete))]
        [XmlArrayItem("Cab", typeof(OeTaskFileArchiverArchiveCab))]
        [XmlArrayItem("Webclient", typeof(OeTaskWebclient))]
        public List<AOeTask> Tasks { get; set; }

        public override List<AOeTask> GetTaskList() => Tasks;

        internal override void InitIds() => InitIds(Tasks);
    }
}