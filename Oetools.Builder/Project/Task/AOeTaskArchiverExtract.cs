using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class that allows to extract files from archives.
    /// </summary>
    /// <inheritdoc cref="AOeTaskFilter"/>
    public abstract class AOeTaskArchiverExtract : AOeTaskFilterAttributes {
        
        /// <inheritdoc cref="AOeTaskArchiverDelete.ArchivePath"/>
        [XmlIgnore]
        public abstract string ArchivePath { get; set; }
        
        /// <summary>
        /// The extraction target file path.
        /// </summary>
        /// <inheritdoc cref="AOeTaskFileArchiverArchive.TargetArchivePath"/>
        [XmlIgnore]
        public abstract string TargetFilePath { get; set; }
        
        /// <summary>
        /// The extraction target directory.
        /// </summary>
        /// <inheritdoc cref="AOeTaskFileArchiverArchive.TargetArchivePath"/>
        [XmlIgnore]
        public abstract string TargetDirectory { get; set; }
        
        /// <summary>
        /// Returns an instance of an archiver.
        /// </summary>
        /// <returns></returns>
        protected abstract IArchiverFullFeatured GetArchiver();
        
        /// <summary>
        /// Must return a new instance of <see cref="AOeTarget"/> corresponding to the current target type.
        /// </summary>
        /// <returns></returns>
        protected abstract AOeTarget GetNewTarget();

        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            if (string.IsNullOrEmpty(TargetFilePath) && TargetDirectory == null) {
                throw new TaskValidationException(this, $"This task needs at least one of the two following properties to be defined : {GetType().GetXmlName(nameof(TargetFilePath))} and/or {GetType().GetXmlName(nameof(TargetDirectory))}.");
            }
            CheckTargetPath(TargetFilePath?.Split(';'), () => GetType().GetXmlName(nameof(TargetFilePath)));
            CheckTargetPath(TargetDirectory?.Split(';'), () => GetType().GetXmlName(nameof(TargetDirectory)));
            
            if (string.IsNullOrEmpty(ArchivePath)) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined : {GetType().GetXmlName(nameof(ArchivePath))}.");
            }
            CheckTargetPath(ArchivePath?.Split(';'), () => GetType().GetXmlName(nameof(ArchivePath)));
            
            base.Validate();
        }

        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected override void ExecuteInternal() {
            var archives = ArchivePath;
            var archiver = GetArchiver();
            
            archiver.SetCancellationToken(CancelToken);
            archiver.OnProgress += ArchiverOnOnProgress;

            try {
                foreach (var archivePath in archives.Split(';')) {
                    var filesToExtract = archiver.ListFiles(archivePath)
                        .Where(f => IsPathPassingFilter(f.PathInArchive))
                        .ToList();

                    if (!filesToExtract.Any()) {
                        Log?.Trace?.Write($"No files found in {archivePath}.");
                        continue;
                    }
                
                    Log?.Trace?.Write($"Extracting {filesToExtract.Count} files from {archivePath}.");
                    
                    archiver.ExtractFileSet(GetFilesToExtract(filesToExtract));
                }
                
            } finally {
                archiver.OnProgress -= ArchiverOnOnProgress;
            }
        }

        /// <inheritdoc cref="AOeTaskFileArchiverArchive.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // this task doesn't actually build anything
        }

        private IEnumerable<IFileInArchiveToExtract> GetFilesToExtract(List<IFileInArchive> filesToExtract) {
            foreach (var f in filesToExtract) {
                foreach (var target in GetTargets(f.PathInArchive, null, null, TargetFilePath, TargetDirectory, GetNewTarget)) {
                    yield return new FileInArchiveToExtract(f.ArchivePath, f.PathInArchive, target.FilePathInArchive);
                }
            }
        }

        private void ArchiverOnOnProgress(object sender, ArchiverEventArgs args) {
            Log?.ReportProgress(100, (int) args.PercentageDone, $"Extracting {args.RelativePathInArchive} from {args.ArchivePath}.");
        }

        private class FileInArchiveToExtract : IFileInArchiveToExtract {
            public string ArchivePath { get; }
            public string PathInArchive { get; }
            public bool Processed { get; set; }
            public string ExtractionPath { get; }
            public FileInArchiveToExtract(string archivePath, string relativePathInArchive, string extractionPath) {
                ArchivePath = archivePath;
                PathInArchive = relativePathInArchive;
                ExtractionPath = extractionPath;
                Processed = false;
            }
        }
    }
}