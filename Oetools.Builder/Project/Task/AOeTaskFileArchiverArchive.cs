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
using Oetools.Builder.Utilities;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class for tasks that operates on files and that have targets for aforementioned files.
    /// </summary>
    public abstract class AOeTaskFileArchiverArchive : AOeTaskFile, IOeTaskFileToBuild {
        
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
        
        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            base.Validate();
            
            // need at least 1 target
            if (string.IsNullOrEmpty(GetTargetFilePath()) && GetTargetDirectory() == null) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined : {GetType().GetXmlName(GetTargetFilePathPropertyName())} and/or {GetType().GetXmlName(GetTargetDirectoryPropertyName())}");
            }
            CheckTargetPath((GetTargetFilePath()?.Split(';')).UnionHandleNull(GetTargetDirectory()?.Split(';')));
            
            // need archive path
            if (!string.IsNullOrEmpty(GetArchivePathPropertyName()) && string.IsNullOrEmpty(GetArchivePath())) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined : {GetType().GetXmlName(GetArchivePathPropertyName())}");
            }
            CheckTargetPath(GetArchivePath()?.Split(';'));
        }
        
        /// <inheritdoc cref="IOeTaskFileToBuild.SetTargets"/>
        public void SetTargets(PathList<IOeFileToBuild> paths, string baseTargetDirectory, bool appendMode = false) {
            bool isTaskCompile = this is IOeTaskCompile;
            foreach (var file in paths) {
                var newTargets = GetTargets(file.Path, baseTargetDirectory, GetArchivePath(), GetTargetFilePath(), GetTargetDirectory(), GetNewTarget);
                        
                // change the targets extension to .r for compiled files.
                if (isTaskCompile && newTargets != null) {
                    foreach (var targetArchive in newTargets) {
                        targetArchive.FilePathInArchive = Path.ChangeExtension(targetArchive.FilePathInArchive, UoeConstants.ExtR);
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
        /// Returns a file built from a file to build, should be called when the action is done for the source file.
        /// In case of a compiled file, this also sets output properties to the file built (db references and required files).
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        private OeFileBuilt GetNewFileBuilt(IOeFile sourceFile) {
            if (this is IOeTaskCompile thisOeTaskCompile) {
                var newFileBuilt = new OeFileBuiltCompiled(sourceFile);
                var compiledFile = thisOeTaskCompile.GetCompiledFiles()?[sourceFile.Path];
                if (compiledFile != null) {
                    newFileBuilt.RequiredFiles = compiledFile.RequiredFiles?.ToList();
                    newFileBuilt.RequiredDatabaseReferences = compiledFile.RequiredDatabaseReferences?.Select(OeDatabaseReference.New).ToList();
                }
                newFileBuilt.Targets = null;
                return newFileBuilt;
            }
            return new OeFileBuilt(sourceFile);
        }

        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected sealed override void ExecuteInternal() {
            if (this is IOeTaskCompile thisOeTaskCompile) {
                try {
                    Log?.Debug("Is a compile task.");
                    var compiledFiles = thisOeTaskCompile.GetCompiledFiles();
                    if (compiledFiles == null) {
                        Log?.Debug("Start file compilation.");
                        compiledFiles = OeFilesCompiler.CompileFiles(thisOeTaskCompile.GetProperties(), _filesToBuild.CopySelect(f => new UoeFileToCompile(f.Path) {
                            FileSize = f.Size
                        }), CancelToken, Log);
                        thisOeTaskCompile.SetCompiledFiles(compiledFiles);
                    }
                    
                    Log?.Debug("Switching original source files for rcode files to build.");
                    _filesToBuild = OeFilesCompiler.SetRcodeFilesAsSourceInsteadOfSourceFiles(_filesToBuild, compiledFiles);
                } catch(Exception e) {
                    throw new TaskExecutionException(this, e.Message, e);
                }
            }
            ExecuteInternalArchive();
        }

        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected virtual void ExecuteInternalArchive() {
            
            var filesToPack = _filesToBuild.SelectMany(f => f.TargetsToBuild.Select(t => new FileToArchive(t.ArchiveFilePath, t.FilePathInArchive, f.PathForTaskExecution, f.Path))).ToList();
                
            Log?.Trace?.Write($"Processing {filesToPack.Count} files.");
            
            var archiver = GetArchiver();
            
            archiver.SetCancellationToken(CancelToken);
            archiver.OnProgress += ArchiverOnProgress;
            try {
                archiver.ArchiveFileSet(filesToPack.Cast<IFileToArchive>());
            } finally {
                archiver.OnProgress -= ArchiverOnProgress;
            }

            // set the files + targets that were actually built
            _builtFiles = new PathList<IOeFileBuilt>();
            foreach (var file in filesToPack.Where(file => file.Processed)) {
                var builtFile = _builtFiles[file.ActualSourcePath];
                if (builtFile == null) {
                    builtFile = GetNewFileBuilt(_filesToBuild[file.ActualSourcePath]);
                    _builtFiles.Add(builtFile);
                }
                if (builtFile.Targets == null) {
                    builtFile.Targets = new List<AOeTarget>();
                }
                var target = GetNewTarget();
                target.ArchiveFilePath = file.ArchivePath;
                target.FilePathInArchive = file.PathInArchive;
                builtFile.Targets.Add(target);
            }
        }
        
        private void ArchiverOnProgress(object sender, ArchiverEventArgs args) {
            var archiveString = string.IsNullOrEmpty(args.ArchivePath) ? "" : $" in {args.ArchivePath}";
            Log?.ReportProgress(100, (int) args.PercentageDone, $"Processing {args.RelativePathInArchive}{archiveString}.");
        }
        
        private struct FileToArchive : IFileToArchive {
            public string ArchivePath { get; }
            public string PathInArchive { get; }
            public bool Processed { get; set; }
            public string SourcePath { get; }
            public string ActualSourcePath { get; }
            public FileToArchive(string archivePath, string pathInArchive, string sourcePath, string actualSourcePath) {
                ArchivePath = archivePath;
                PathInArchive = pathInArchive;
                SourcePath = sourcePath;
                ActualSourcePath = actualSourcePath;
                Processed = false;
            }
        }
        
        /// <summary>
        /// Adds all the files to build to the built files list,
        /// this method is executed instead of <see cref="AOeTask.ExecuteInternal"/> when test mode is on.
        /// </summary>
        protected override void ExecuteTestModeInternal() {
            _builtFiles = new PathList<IOeFileBuilt>();
            foreach (var file in GetFilesToBuild()) {
                var fileBuilt = GetNewFileBuilt(file);
                fileBuilt.Targets = file.TargetsToBuild.ToList();
                _builtFiles.Add(fileBuilt);
            }
        }

        private string _baseDirectory;

        /// <inheritdoc cref="IOeTaskFileToBuild.SetBaseDirectory"/>
        public void SetBaseDirectory(string baseDirectory) {
            _baseDirectory = baseDirectory;
        }
        
        private PathList<IOeFileToBuild> _filesToBuild;
        
        /// <inheritdoc cref="IOeTaskFile.SetFilesToProcess"/>
        public override void SetFilesToProcess(PathList<IOeFile> filesToProcess) {
            // make a deep copy of those files because we will modify them
            _filesToBuild = filesToProcess.CopySelect(f => new OeFile(f) as IOeFileToBuild);
            SetTargets(_filesToBuild, _baseDirectory);
        }

        /// <inheritdoc cref="IOeTaskFileToBuild.GetFilesToBuild"/>
        public PathList<IOeFileToBuild> GetFilesToBuild() => _filesToBuild;
        
        /// <inheritdoc cref="IOeTaskFile.GetFilesToProcess"/>
        public override PathList<IOeFile> GetFilesToProcess() => _filesToBuild.CopySelect(f => f as IOeFile);

        #region IOeTaskCompile

        private PathList<UoeCompiledFile> CompiledPaths { get; set; }
        
        /// <inheritdoc cref="IOeTaskCompile.SetCompiledFiles"/>
        public void SetCompiledFiles(PathList<UoeCompiledFile> compiledPath) => CompiledPaths = compiledPath;
        
        /// <inheritdoc cref="IOeTaskCompile.GetCompiledFiles"/>
        public PathList<UoeCompiledFile> GetCompiledFiles() => CompiledPaths;

        #endregion

        #region IOeTaskWithBuiltFiles

        private PathList<IOeFileBuilt> _builtFiles;
        
        /// <inheritdoc cref="IOeTaskWithBuiltFiles.GetBuiltFiles"/>
        public PathList<IOeFileBuilt> GetBuiltFiles() => _builtFiles;

        #endregion
    }
}