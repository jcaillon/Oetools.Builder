#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTargetArchiveCab.cs) is part of Oetools.Builder.
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
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.History;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Archive;

namespace Oetools.Builder.Project.Task {
    
    [Serializable]
    [XmlRoot("Move")]
    public class OeTaskFileMove : AOeTaskFileArchiverArchive {
        
        // ReSharper disable once NotAccessedField.Local
        private string _targetArchivePath;

        /// <inheritdoc cref="AOeTaskFileArchiverArchive.TargetFilePath"/>
        [XmlAttribute("TargetFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public override string TargetFilePath { get; set; }
        
        /// <inheritdoc cref="AOeTaskFileArchiverArchive.TargetDirectory"/>
        [XmlAttribute("TargetDirectory")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public override string TargetDirectory { get; set; }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [XmlIgnore]
        public override string TargetArchivePath {
            get => null;
            set => _targetArchivePath = value;
        }

        protected override bool IsTargetArchiveRequired() => false;

        protected override IArchiver GetArchiver() => Archiver.NewFileSystemArchiver();
        
        protected override AOeTarget GetNewTarget() => new OeTargetFile();
        
        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected override void ExecuteInternalArchive() {
            
            var filesToArchive = GetFilesToBuild().SelectMany(f => f.TargetsToBuild.Select(t => new FileInArchiveToMove(null, f.PathForTaskExecution, t.FilePathInArchive, f.Path))).ToList();
                
            Log?.Trace?.Write($"Processing {filesToArchive.Count} files.");
            
            var archiver = Archiver.NewFileSystemArchiver();
            
            archiver.SetCancellationToken(CancelToken);
            archiver.OnProgress += ArchiverOnProgress;
            try {
                archiver.MoveFileSet(filesToArchive);
            } finally {
                archiver.OnProgress -= ArchiverOnProgress;
            }
        }
        
        private class FileInArchiveToMove : IFileInArchiveToMove {
            public string ArchivePath { get; }
            public string PathInArchive { get; }
            public bool Processed { get; set; }
            public string NewRelativePathInArchive { get; }
            public string ActualSourcePath { get; }
            public FileInArchiveToMove(string archivePath, string pathInArchive, string newRelativePathInArchive, string actualSourcePath) {
                ArchivePath = archivePath;
                PathInArchive = pathInArchive;
                NewRelativePathInArchive = newRelativePathInArchive;
                ActualSourcePath = actualSourcePath;
            }
        }
    }
}