using System.Collections.Generic;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class that allows to move files within archives.
    /// </summary>
    public abstract class AOeTaskArchiverMove : AOeTaskFilter {
        
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
        /// The file path to which we need to move the included files.
        /// </summary>
        /// <remarks>
        /// This string can contain ; to separate several values.
        /// Each value can contain placeholders.
        /// </remarks>
        /// <returns></returns>
        protected abstract string GetTargetFilePath();
        
        /// <summary>
        /// Returns the property name of the property which holds the value <see cref="GetTargetFilePath"/>.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetTargetFilePathPropertyName();
        
        /// <summary>
        /// The directory to which we need to move the included files.
        /// </summary>
        /// <remarks>
        /// This string can contain ; to separate several values.
        /// Each value can contain placeholders.
        /// </remarks>
        /// <returns></returns>
        protected abstract string GetTargetDirectory();
        
        /// <summary>
        /// Returns the property name of the property which holds the value <see cref="GetTargetDirectory"/>.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetTargetDirectoryPropertyName();
        
        /// <summary>
        /// Returns an instance of an archiver.
        /// </summary>
        /// <returns></returns>
        protected abstract IArchiver GetArchiver();
        
        /// <summary>
        /// Must return a new instance of <see cref="AOeTarget"/> corresponding to the current target type.
        /// </summary>
        /// <returns></returns>
        protected abstract AOeTarget GetNewTarget();

        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            if (string.IsNullOrEmpty(GetTargetFilePath()) && GetTargetDirectory() == null) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined : {GetType().GetXmlName(GetTargetFilePathPropertyName())} and/or {GetType().GetXmlName(GetTargetDirectoryPropertyName())}");
            }
            if (string.IsNullOrEmpty(GetArchivePath())) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined : {GetType().GetXmlName(GetArchivePathPropertyName())}");
            }
            CheckTargetPath((GetTargetFilePath()?.Split(';')).UnionHandleNull(GetTargetDirectory()?.Split(';')));
            CheckTargetPath(GetArchivePath()?.Split(';'));
            base.Validate();
        }

        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected override void ExecuteInternal() {
            var archives = GetArchivePath();
            var archiver = GetArchiver();
            
            archiver.SetCancellationToken(CancelToken);
            archiver.OnProgress += ArchiverOnOnProgress;

            try {
                foreach (var archivePath in archives.Split(';')) {
                    var filesToMove = archiver.ListFiles(archivePath)
                        .Where(f => IsPathPassingFilter(f.PathInArchive))
                        .ToList();

                    if (!filesToMove.Any()) {
                        Log?.Trace?.Write($"No files found in {archivePath}.");
                        continue;
                    }
                
                    Log?.Trace?.Write($"Moving {filesToMove.Count} files within {archivePath}.");
                    
                    archiver.MoveFileSet(GetFilesToMove(filesToMove));
                }
            } finally {
                archiver.OnProgress -= ArchiverOnOnProgress;
            }
        }

        /// <inheritdoc cref="AOeTaskFileArchiverArchive.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // this task doesn't actually build anything
        }

        private IEnumerable<IFileInArchiveToMove> GetFilesToMove(List<IFileInArchive> filesToExtract) {
            foreach (var f in filesToExtract) {
                foreach (var target in GetTargets(f.PathInArchive, null, f.ArchivePath, GetTargetFilePath(), GetTargetDirectory(), GetNewTarget)) {
                    yield return new FileInArchiveToMove(f.ArchivePath, f.PathInArchive, target.FilePathInArchive);
                }
            }
        }

        private void ArchiverOnOnProgress(object sender, ArchiverEventArgs args) {
            Log?.ReportProgress(100, (int) args.PercentageDone, $"Extracting {args.RelativePathInArchive} from {args.ArchivePath}.");
        }

        private struct FileInArchiveToMove : IFileInArchiveToMove {
            public string ArchivePath { get; }
            public string PathInArchive { get; }
            public bool Processed { get; set; }
            public string NewRelativePathInArchive { get; }
            public FileInArchiveToMove(string archivePath, string relativePathInArchive, string newRelativePathInArchive) {
                ArchivePath = archivePath;
                PathInArchive = relativePathInArchive;
                NewRelativePathInArchive = newRelativePathInArchive;
                Processed = false;
            }
        }
    }
}