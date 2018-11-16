using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class that allows to delete files within archives.
    /// </summary>
    /// <inheritdoc cref="AOeTaskFilter"/>
    public abstract class AOeTaskArchiverDelete : AOeTaskFilter {
        
        /// <summary>
        /// The path of archive to modify.
        /// </summary>
        /// <remarks>
        /// Several target paths can be used, separate them with a semi-colon (i.e. ;).
        /// </remarks>
        [XmlIgnore]
        public abstract string ArchivePath { get; set; }
        
        /// <summary>
        /// Returns an instance of an archiver.
        /// </summary>
        /// <returns></returns>
        protected abstract IArchiverFullFeatured GetArchiver();

        public override void Validate() {
            if (string.IsNullOrEmpty(ArchivePath)) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined : {GetType().GetXmlName(nameof(ArchivePath))}.");
            }
            CheckTargetPath(ArchivePath?.Split(';'), () => GetType().GetXmlName(nameof(ArchivePath)));
            
            base.Validate();
        }

        protected override void ExecuteInternal() {
            var archives = ArchivePath;
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

        private class FileInArchiveToDelete : IFileInArchiveToDelete {
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