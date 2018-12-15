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
using System.Xml.Serialization;
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
    /// <inheritdoc cref="AOeTaskFilter"/>
    public abstract class AOeTaskFileArchiverArchive : AOeTaskFile, IOeTaskFileToBuild {
                
        /// <summary>
        /// The path to the targeted archive.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Several target paths can be used, separate them with a semi-colon (i.e. ;).
        /// Each target path can use special placeholders:
        /// - {{FILE_SOURCE_DIRECTORY}} will be replaced by the source directory of the file processed
        /// - {{group_name}} will be replaced by the value captured in group "group_name"
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>
        /// Having "((C:\folder\**))((*.txt))" as an include pattern
        /// and "D:\pre_{{2}}.raw" as the target,
        /// for the file "C:\folder\myfile.txt", we will have the target "D:\pre_myfile.raw".
        /// (note: In this example, the captured group 1 was not used)
        /// </para>
        /// </example>
        /// <returns></returns>
        [XmlIgnore]
        public abstract string TargetArchivePath { get; set; }
        
        /// <summary>
        /// The relative target file path inside the archive.
        /// </summary>
        /// <inheritdoc cref="TargetArchivePath"/>
        [XmlIgnore]
        public abstract string TargetFilePath { get; set; }
        
        /// <summary>
        /// The relative target directory inside the archive.
        /// </summary>
        /// <inheritdoc cref="TargetArchivePath"/>
        [XmlIgnore]
        public abstract string TargetDirectory { get; set; }
        
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

        /// <summary>
        /// Returns true of the <see cref="TargetArchivePath"/> is required for this task to operate.
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsTargetArchiveRequired() => true;
        
        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            // need at least 1 target
            if (string.IsNullOrEmpty(TargetFilePath) && TargetDirectory == null) {
                throw new TaskValidationException(this, $"This task needs at least one of the two following properties to be defined : {GetType().GetXmlName(nameof(TargetFilePath))} and/or {GetType().GetXmlName(nameof(TargetDirectory))}.");
            }
            CheckTargetPath(TargetFilePath?.Split(';'), () => GetType().GetXmlName(nameof(TargetFilePath)));
            CheckTargetPath(TargetDirectory?.Split(';'), () => GetType().GetXmlName(nameof(TargetDirectory)));
            
            // need archive path
            if (IsTargetArchiveRequired() && string.IsNullOrEmpty(TargetArchivePath)) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined : {GetType().GetXmlName(nameof(TargetArchivePath))}.");
            }
            CheckTargetPath(TargetArchivePath?.Split(';'), () => GetType().GetXmlName(nameof(TargetArchivePath)));
            

            base.Validate();
        }
        
        /// <inheritdoc cref="IOeTaskFileToBuild.SetTargets"/>
        public void SetTargets(PathList<IOeFileToBuild> paths, string baseTargetDirectory, bool appendMode = false) {
            bool isTaskCompile = this is IOeTaskCompile;
            foreach (var file in paths) {
                var newTargets = GetTargets(file.Path, baseTargetDirectory, TargetArchivePath, TargetFilePath, TargetDirectory, GetNewTarget);
                        
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
        /// Adds all the files to build to the built files list,
        /// this method is executed instead of <see cref="AOeTask.ExecuteInternal"/> when test mode is on.
        /// </summary>
        protected override void ExecuteTestModeInternal() {
            _builtFiles = new PathList<IOeFileBuilt>();
            foreach (var file in GetFilesToBuild()) {
                var fileBuilt = GetNewFileBuilt(file);
                fileBuilt.Targets = file.TargetsToBuild?.ToList();
                _builtFiles.Add(fileBuilt);
            }
        }

        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected sealed override void ExecuteInternal() {
            OeFilesCompiler compiler = null;
            if (this is IOeTaskCompile thisOeTaskCompile) {
                try {
                    Log?.Debug("Is a compile task.");
                    var compiledFiles = thisOeTaskCompile.GetCompiledFiles();
                    if (compiledFiles == null) {
                        Log?.Debug("Start file compilation.");
                        compiler = new OeFilesCompiler();
                        compiledFiles = compiler.CompileFiles(thisOeTaskCompile.GetProperties(), _filesToBuild.CopySelect(f => new UoeFileToCompile(f.Path) {
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
            try {
                ExecuteInternalArchive();
            } finally {
                compiler?.Dispose();
            }
        }

        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected virtual void ExecuteInternalArchive() {
            
            var filesToArchive = _filesToBuild.SelectMany(f => f.TargetsToBuild.ToNonNullEnumerable().Select(t => new FileToArchive(t.ArchiveFilePath, t.FilePathInArchive, f.PathForTaskExecution, f.Path))).ToList();
                
            Log?.Trace?.Write($"Processing {filesToArchive.Count} files.");
            
            var archiver = GetArchiver();
            archiver.SetCancellationToken(CancelToken);
            archiver.OnProgress += ArchiverOnProgress;
            try {
                archiver.ArchiveFileSet(filesToArchive);
            } finally {
                archiver.OnProgress -= ArchiverOnProgress;
            }

            AddBuiltFiles(filesToArchive);
        }

        private void AddBuiltFiles(IEnumerable<FileToArchive> filesToArchive) {
            // set the files + targets that were actually built
            _builtFiles = new PathList<IOeFileBuilt>();
            foreach (var file in filesToArchive) {
                var builtFile = _builtFiles[file.ActualSourcePath];
                if (builtFile == null) {
                    builtFile = GetNewFileBuilt(_filesToBuild[file.ActualSourcePath]);
                    _builtFiles.Add(builtFile);
                }

                // file processed, add targets
                if (file.Processed) {
                    if (builtFile.Targets == null) {
                        builtFile.Targets = new List<AOeTarget>();
                    }
                    var target = GetNewTarget();
                    target.ArchiveFilePath = file.ArchivePath;
                    target.FilePathInArchive = file.PathInArchive;
                    builtFile.Targets.Add(target);
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
                var newFileBuilt = new OeFileBuilt(sourceFile);
                var compiledFile = thisOeTaskCompile.GetCompiledFiles()?[sourceFile.Path];
                if (compiledFile != null) {
                    newFileBuilt.RequiredFiles = compiledFile.RequiredFiles?.ToList();
                    newFileBuilt.RequiredDatabaseReferences = compiledFile.RequiredDatabaseReferences?.Select(OeDatabaseReference.New).ToList();
                    if (compiledFile.CompilationProblems != null && compiledFile.CompilationProblems.Count > 0) {
                        newFileBuilt.CompilationProblems = compiledFile.CompilationProblems.Select(AOeCompilationProblem.New).ToList();
                    }
                }
                return newFileBuilt;
            }
            return new OeFileBuilt(sourceFile);
        }
        
        protected void ArchiverOnProgress(object sender, ArchiverEventArgs args) {
            var archiveString = string.IsNullOrEmpty(args.ArchivePath) ? "" : $" in {args.ArchivePath}";
            Log?.ReportProgress(100, (int) args.PercentageDone, $"Processing {args.RelativePathInArchive}{archiveString}.");
        }
        
        protected class FileToArchive : IFileToArchive {
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

        private string _targetBaseDirectory;

        /// <inheritdoc cref="IOeTaskFileToBuild.SetTargetBaseDirectory"/>
        public void SetTargetBaseDirectory(string baseDirectory) {
            _targetBaseDirectory = baseDirectory;
        }
        
        protected PathList<IOeFileToBuild> _filesToBuild;
        
        /// <inheritdoc cref="IOeTaskFile.SetFilesToProcess"/>
        public override void SetFilesToProcess(PathList<IOeFile> filesToProcess) {
            // make a deep copy of those files because we will modify them
            _filesToBuild = filesToProcess.CopySelect(f => new OeFile(f) as IOeFileToBuild);
            SetTargets(_filesToBuild, _targetBaseDirectory);
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

        protected PathList<IOeFileBuilt> _builtFiles;
        
        /// <inheritdoc cref="IOeTaskWithBuiltFiles.GetBuiltFiles"/>
        public PathList<IOeFileBuilt> GetBuiltFiles() => _builtFiles;

        #endregion

    }
}