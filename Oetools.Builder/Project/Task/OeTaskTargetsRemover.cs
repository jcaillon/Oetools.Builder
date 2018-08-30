#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskExec.cs) is part of Oetools.Builder.
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
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// The job of this task is to delete al the targets present in <see cref="FilesWithTargetsToRemove"/> with <see cref="OeTarget.DeletionMode"/> = true, they are
    /// targets that are no longer needed. Those targets were built in the previous build but the targets have changed (or the file itself has been deleted)
    /// </summary>
    [Serializable]
    [XmlRoot("TargetsRemover")]
    public class OeTaskTargetsRemover : OeTask, IOeTaskFileBuilder {

        private FileList<OeFileBuilt> _builtFiles;
        
        protected sealed override void ExecuteInternal() {
            var targetsToRemove = FilesWithTargetsToRemove.SelectMany(f => f.Targets).Where(target => target.IsDeletionMode()).ToList();
            ExecuteTargetsRemoval(targetsToRemove);
            _builtFiles = FilesWithTargetsToRemove;
        }

        private void ExecuteTargetsRemoval(List<OeTarget> targetsToRemove) {
            var fileTargets = targetsToRemove.Where(target => target is OeTargetFileCopy);
            Log?.Debug("Deleting all file targets");

            var archiveTargets = targetsToRemove.Where(target => target is OeTargetArchive);
            Log?.Debug("Deleting all archive targets");
        }
        
        /// <summary>
        /// a list of files with targets to remove
        /// </summary>
        [XmlIgnore]
        internal FileList<OeFileBuilt> FilesWithTargetsToRemove { get; set; }

        public FileList<OeFileBuilt> GetFilesBuilt() => _builtFiles;


    }
}