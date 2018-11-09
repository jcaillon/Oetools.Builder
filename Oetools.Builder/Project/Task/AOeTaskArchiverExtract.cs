﻿using System.Collections.Generic;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class that allows to extract files from archives.
    /// </summary>
    public abstract class AOeTaskArchiverExtract : AOeTaskFilter {
        
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
        /// The file path to which we need to extract the included files.
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
        /// The directory to which we need to extract the included files.
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
                    var filesToExtract = archiver.ListFiles(archivePath)
                        .Where(f => IsPathPassingFilter(f.RelativePathInArchive))
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
                foreach (var target in GetTargets(f.RelativePathInArchive, null, null, GetTargetFilePath(), GetTargetDirectory(), GetNewTarget)) {
                    yield return new FileInArchiveToExtract(f.ArchivePath, f.RelativePathInArchive, target.FilePath);
                }
            }
        }

        private void ArchiverOnOnProgress(object sender, ArchiverEventArgs args) {
            if (args.EventType == ArchiverEventType.GlobalProgression) {
                Log?.ReportProgress(100, (int) args.PercentageDone, $"Extracting {args.RelativePathInArchive} from {args.ArchivePath}.");
            }
        }

        private struct FileInArchiveToExtract : IFileInArchiveToExtract {
            public string ArchivePath { get; }
            public string RelativePathInArchive { get; }
            public string ExtractionPath { get; }
            public FileInArchiveToExtract(string archivePath, string relativePathInArchive, string extractionPath) {
                ArchivePath = archivePath;
                RelativePathInArchive = relativePathInArchive;
                ExtractionPath = extractionPath;
            }
        }
    }
}