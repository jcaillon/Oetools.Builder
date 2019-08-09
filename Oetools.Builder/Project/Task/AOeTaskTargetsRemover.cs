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
using DotUtilities;
using DotUtilities.Archive;
using DotUtilities.Extensions;
using Oetools.Builder.History;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// This task deletes al the targets present in <see cref="_pathsWithTargetsToRemove"/>, they are
    /// targets that are no longer needed. Those targets were built in the previous build but the targets have changed (or the file itself has been deleted)
    /// </summary>
    public class AOeTaskTargetsRemover : AOeTask {

        /// <summary>
        /// a list of files with targets to remove
        /// </summary>
        private PathList<IOeFileBuilt> _pathsWithTargetsToRemove;

        public PathList<IOeFileBuilt> GetRemovedTargets() => _pathsWithTargetsToRemove;

        public void SetFilesWithTargetsToRemove(PathList<IOeFileBuilt> pathsWithTargetsToRemove) {
            _pathsWithTargetsToRemove = pathsWithTargetsToRemove;
        }

        /// <inheritdoc cref="AOeTask.Validate"/>
        public override void Validate() {
            // nothing to validate
        }

        /// <inheritdoc cref="AOeTask.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {

        }

        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected sealed override void ExecuteInternal() {
            if (_pathsWithTargetsToRemove == null) {
                return;
            }
            Log?.Debug("Deleting all archive targets.");

            List<FileInArchiveToDelete> targetsToDelete = new List<FileInArchiveToDelete>();
            foreach (var file in _pathsWithTargetsToRemove) {
                foreach (var target in file.Targets.ToNonNullEnumerable()) {
                    targetsToDelete.Add(new FileInArchiveToDelete {
                        ArchivePath = target.ArchiveFilePath,
                        PathInArchive = target.FilePathInArchive,
                        SourceFile = file,
                        TargetType = target.GetType()
                    });
                }
            }
            foreach (var groupedTargets in targetsToDelete.GroupBy(target => target.TargetType)) {
                var archiver = AOeTarget.GetArchiverDelete(groupedTargets.Key);
                if (archiver == null) {
                    continue;
                }
                archiver.DeleteFileSet(targetsToDelete);
            }
        }

        private class FileInArchiveToDelete : IFileInArchiveToDelete {
            public string ArchivePath { get; set; }
            public string PathInArchive { get; set; }
            public bool Processed { get; set; }
            public IOeFileBuilt SourceFile { get; set; }
            public Type TargetType { get; set; }
        }

    }
}
