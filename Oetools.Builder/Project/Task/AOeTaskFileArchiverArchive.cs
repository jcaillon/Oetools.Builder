#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTarget.cs) is part of Oetools.Builder.
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
using System.IO;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class for tasks that operates on files and that have targets for aforementioned files.
    /// </summary>
    public abstract class AOeTaskFileArchiverArchive : AOeTaskFile, IOeTaskFileWithTargets {

        /// <summary>
        /// Compression level for this archive task.
        /// </summary>
        /// <returns></returns>
        public abstract ArchiveCompressionLevel GetCompressionLevel();
        
        /// <summary>
        /// Returns an instance of an archiver.
        /// </summary>
        /// <returns></returns>
        protected abstract IArchiver GetArchiver();
        
        /// <summary>
        /// The path of the target archive.
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
        /// Must return a new instance of <see cref="AOeTarget"/> corresponding to the current target type.
        /// </summary>
        /// <returns></returns>
        protected abstract AOeTarget GetNewTarget();
        
        /// <summary>
        /// The relative target file path inside the archive.
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
        /// The relative target directory inside the archive.
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
        
        protected PathList<OeFileBuilt> _builtPaths;
        
        /// <inheritdoc cref="IOeTaskWithBuiltFiles.GetBuiltFiles"/>
        public PathList<OeFileBuilt> GetBuiltFiles() => _builtPaths;
        
        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            base.Validate();
            if (string.IsNullOrEmpty(GetTargetFilePath()) && GetTargetDirectory() == null) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined : {GetType().GetXmlName(GetTargetFilePathPropertyName())} and/or {GetType().GetXmlName(GetTargetDirectoryPropertyName())}");
            }
            if (!string.IsNullOrEmpty(GetArchivePathPropertyName()) && string.IsNullOrEmpty(GetArchivePath())) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined : {GetType().GetXmlName(GetArchivePathPropertyName())}");
            }
            CheckTargetPath((GetTargetFilePath()?.Split(';')).UnionHandleNull(GetTargetDirectory()?.Split(';')));
            CheckTargetPath(GetArchivePath()?.Split(';'));
        }
        
        /// <inheritdoc cref="IOeTaskFileWithTargets.SetTargets"/>
        public void SetTargets(PathList<OeFile> paths, string baseTargetDirectory, bool appendMode = false) {
            bool isTaskCompile = this is IOeTaskCompile;
            foreach (var file in paths) {
                var newTargets = GetTargets(file.Path, baseTargetDirectory, GetArchivePath(), GetTargetFilePath(), GetTargetDirectory(), GetNewTarget);
                        
                // change the targets extension to .r for compiled files.
                if (isTaskCompile && newTargets != null) {
                    foreach (var targetArchive in newTargets) {
                        targetArchive.FilePath = Path.ChangeExtension(targetArchive.FilePath, UoeConstants.ExtR);
                    }
                }
                        
                if (appendMode && file.TargetsToBuild != null) {
                    if (newTargets != null) {
                        file.TargetsToBuild.AddRange(newTargets);
                    }
                } else {
                    file.TargetsToBuild = newTargets;
                }
            }
        }
        
        /// <summary>
        /// Adds all the files to build to the built files list,
        /// this method is executed instead of <see cref="AOeTask.ExecuteInternal"/> when test mode is on.
        /// </summary>
        protected override void ExecuteTestModeInternal() {
            _builtPaths = new PathList<OeFileBuilt>();
            foreach (var file in GetFilesToBuild()) {
                var fileBuilt = GetNewFileBuilt(file);
                fileBuilt.Targets = file.TargetsToBuild.ToList();
                _builtPaths.Add(fileBuilt);
            }
        }

        /// <summary>
        /// Returns a file built from a file to build, should be called when the action is done for the source file.
        /// In case of a compiled file, this also sets output properties to the file built (db references and required files).
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        private OeFileBuilt GetNewFileBuilt(OeFile sourceFile) {
            if (this is IOeTaskCompile thisOeTaskCompile) {
                var newFileBuilt = new OeFileBuiltCompiled(sourceFile);
                var compiledFile = thisOeTaskCompile.GetCompiledFiles()?[sourceFile.Path];
                if (compiledFile != null) {
                    newFileBuilt.RequiredFiles = compiledFile.RequiredFiles?.ToList();
                    newFileBuilt.RequiredDatabaseReferences = compiledFile.RequiredDatabaseReferences?.Select(OeDatabaseReference.New).ToList();
                }
                return newFileBuilt;
            }
            return new OeFileBuilt(sourceFile);
        }

        /// <inheritdoc cref="AOeTaskFile.ExecuteForFilesInternal"/>
        protected sealed override void ExecuteForFilesInternal(IEnumerable<IOeFile> paths) => throw new Exception("Should not be called.");

        /// <inheritdoc cref="IOeTaskFileWithTargets.ExecuteForFilesWithTargets"/>
        public virtual void ExecuteForFilesWithTargets(IEnumerable<IOeFileToBuild> files) {
            var archiver = GetArchiver();
            
            archiver.SetCancellationToken(CancelToken);
            archiver.SetCompressionLevel(GetCompressionLevel());
            archiver.OnProgress += ArchiverOnProgress;
            
            try {
                var filesToPack = files.SelectMany(f => f.TargetsToBuild.Select(t => new FileToArchive(t.ArchiveFilePath, t.FilePath, f.SourcePathForTaskExecution) as IFileToArchive)).ToList();
                
                Log?.Trace?.Write($"Processing {filesToPack.Count} files.");
                
                archiver.ArchiveFileSet(filesToPack);
                
            } finally {
                archiver.OnProgress -= ArchiverOnProgress;
            }
        }       
        
        private void ArchiverOnProgress(object sender, ArchiverEventArgs args) {
            switch (args.EventType) {
                case ArchiverEventType.GlobalProgression:
                    var archiveString = string.IsNullOrEmpty(args.ArchivePath) ? "" : $" in {args.ArchivePath}";
                    Log?.ReportProgress(100, (int) args.PercentageDone, $"Processing {args.RelativePathInArchive}{archiveString}.");
                    break;
                case ArchiverEventType.FileProcessed:
                    var archiveString2 = string.IsNullOrEmpty(args.ArchivePath) ? "" : $" in {args.ArchivePath}";
                    Log?.Trace?.Write($"{args.RelativePathInArchive}{archiveString2} has been processed.");
                    break;
            }
        }
        
        private struct FileToArchive : IFileToArchive {
            public string ArchivePath { get; }
            public string RelativePathInArchive { get; }
            public string SourcePath { get; }
            public FileToArchive(string archivePath, string relativePathInArchive, string sourcePath) {
                ArchivePath = archivePath;
                RelativePathInArchive = relativePathInArchive;
                SourcePath = sourcePath;
            }
        }
        
        private PathList<UoeCompiledFile> CompiledPaths { get; set; }
        
        /// <inheritdoc cref="IOeTaskCompile.SetCompiledFiles"/>
        public void SetCompiledFiles(PathList<UoeCompiledFile> compiledPath) => CompiledPaths = compiledPath;
        
        /// <inheritdoc cref="IOeTaskCompile.GetCompiledFiles"/>
        public PathList<UoeCompiledFile> GetCompiledFiles() => CompiledPaths;
        
    }
}