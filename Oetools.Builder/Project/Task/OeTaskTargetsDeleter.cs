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
    /// The job of this task is to delete al the targets present in <see cref="_pathsWithTargetsToRemove"/> with <see cref="AOeTarget.DeletionMode"/> = true, they are
    /// targets that are no longer needed. Those targets were built in the previous build but the targets have changed (or the file itself has been deleted)
    /// </summary>
    [Serializable]
    [XmlRoot("TargetsRemover")]
    public class OeTaskTargetsDeleter : AOeTask, IOeTaskWithBuiltFiles {

        private PathList<OeFileBuilt> _builtPaths;

        /// <summary>
        /// a list of files with targets to remove
        /// </summary>
        private PathList<OeFileBuilt> _pathsWithTargetsToRemove;

        public void SetFilesWithTargetsToRemove(PathList<OeFileBuilt> pathsWithTargetsToRemove) {
            _pathsWithTargetsToRemove = pathsWithTargetsToRemove;
        }

        /// <inheritdoc cref="AOeTask.Validate"/>
        public override void Validate() {
            // nothing to validate
        }

        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected sealed override void ExecuteInternal() {
            var targetsToRemove = _pathsWithTargetsToRemove.SelectMany(f => f.TargetsToBuild).Where(target => target.IsDeletionMode()).ToList();
            ExecuteTargetsRemoval(targetsToRemove);
            _builtPaths = _pathsWithTargetsToRemove;
        }

        /// <inheritdoc cref="AOeTask.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            _builtPaths = _pathsWithTargetsToRemove;
        }

        private void ExecuteTargetsRemoval(List<AOeTarget> targetsToRemove) {
            var archiveTargets = targetsToRemove;
            Log?.Debug("Deleting all archive targets.");
        }

        /// <inheritdoc cref="IOeTaskWithBuiltFiles.GetBuiltFiles"/>
        public PathList<OeFileBuilt> GetBuiltFiles() => _builtPaths;

    }
}