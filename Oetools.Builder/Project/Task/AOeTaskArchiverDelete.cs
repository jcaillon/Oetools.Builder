using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class that allows to delete files within archives.
    /// </summary>
    public abstract class AOeTaskArchiverDelete : AOeTaskFilter {
        
        /// <summary>
        /// The path of the archive to handle.
        /// </summary>
        /// <remarks>
        /// This string can contain ; to separate several values.
        /// Each value can contain placeholders.
        /// </remarks>
        /// <returns></returns>
        protected abstract string GetArchivePath();
        
        /// <summary>
        /// Returns the property name of the property which holds the value <see cref="GetArchivePath"/>.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetArchivePathPropertyName();

        /// <summary>
        /// Returns an instance of an archiver.
        /// </summary>
        /// <returns></returns>
        protected abstract IArchiver GetArchiver();

        public override void Validate() {
            base.Validate();
            foreach (var archivePath in GetArchivePath().Split(';')) {
                Utils.ValidatePathWildCard(archivePath);
            }
        }

        protected override void ExecuteInternal() {
            var archives = GetArchivePath();
            var archiver = GetArchiver();
            
            archiver.SetCancellationToken(CancelToken);
            archiver.OnProgress += ArchiverOnOnProgress;

            try {
                foreach (var archivePath in archives.Split(';')) {
                    var filesToDelete = archiver.ListFiles(archivePath)
                        .Where(f => IsPathPassingFilter(f.PathInArchive))
                        .ToList();

                    if (!filesToDelete.Any()) {
                        Log?.Trace?.Write($"No files found in {archivePath}.");
                        continue;
                    }
                
                    Log?.Trace?.Write($"Deleting {filesToDelete.Count} files in {archivePath}.");
            

                    archiver.DeleteFileSet(filesToDelete.Select(f => new FileInArchiveToDelete(f.ArchivePath, f.PathInArchive) as IFileInArchiveToDelete));
                }
                
            } finally {
                archiver.OnProgress -= ArchiverOnOnProgress;
            }
        }

        private void ArchiverOnOnProgress(object sender, ArchiverEventArgs args) {
            Log?.ReportProgress(100, (int) args.PercentageDone, $"Deleting {args.RelativePathInArchive} in {args.ArchivePath}.");
        }

        /// <inheritdoc cref="AOeTaskFileArchiverArchive.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // this task doesn't actually build anything, it just deletes files
        }

        private struct FileInArchiveToDelete : IFileInArchiveToDelete {
            public string ArchivePath { get; }
            public string PathInArchive { get; }
            public bool Processed { get; set; }

            public FileInArchiveToDelete(string archivePath, string relativePathInArchive) {
                ArchivePath = archivePath;
                PathInArchive = relativePathInArchive;
                Processed = false;
            }
        }
    }
}