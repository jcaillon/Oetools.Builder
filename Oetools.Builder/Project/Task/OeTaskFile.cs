﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFile.cs) is part of Oetools.Builder.
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
using System.IO;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {

    public abstract class OeTaskFile : OeTaskFilter, IOeTaskFile {
        
        private FileList<OeFile> _filesToBuild;
        
        protected FileList<OeFileBuilt> _builtFiles;
        
        /// <inheritdoc cref="IOeTaskFileBuilder.GetFilesBuilt"/>
        public FileList<OeFileBuilt> GetFilesBuilt() => _builtFiles;
        
        public void SetFilesToBuild(FileList<OeFile> filesToBuild) {
            _filesToBuild = filesToBuild;
        }

        public FileList<OeFile> GetFilesToBuild() => _filesToBuild;

        /// <inheritdoc cref="IOeTask.Validate"/>
        public override void Validate() {
            if (string.IsNullOrEmpty(Include) && string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"This task needs the following properties to be defined or it will not do anything : {GetType().GetXmlName(nameof(Include))} and/or {GetType().GetXmlName(nameof(IncludeRegex))}");
            }
            base.Validate();
        }

        /// <summary>
        /// Validates that this task as at least one <see cref="OeTaskFilter.Include"/> defined and
        /// no <see cref="OeTaskFilter.IncludeRegex"/>
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
        public void ValidateCanIncludeFiles() {
            if (string.IsNullOrEmpty(Include)) {
                throw new TaskValidationException(this, $"This task needs to have the property {GetType().GetXmlName(nameof(Include))} defined or it can not be applied on any file");
            }
            if (!string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"The property {GetType().GetXmlName(nameof(IncludeRegex))} is not allowed for this task because it would not allow to find files to include (it would require to list the entire content of all the discs on this computer to match this regular expression), use the property {GetType().GetXmlName(nameof(Include))} instead");
            }
        }
        
        /// <inheritdoc cref="IOeTaskFile.GetIncludedFiles"/>
        public FileList<OeFile> GetIncludedFiles() {
            var output = new FileList<OeFile>();
            var i = 0;
            foreach (var path in GetIncludeStrings()) {
                if (File.Exists(path)) {
                    // the include directly designate a file
                    if (!IsFileExcluded(path)) {
                        output.TryAdd(new OeFile { FilePath = Path.GetFullPath(path) });
                    }
                } else {
                    // the include is a wildcard path, we try to get the "root" folder to list to get all the files
                    var validDir = Utils.GetLongestValidDirectory(path);
                    if (!string.IsNullOrEmpty(validDir)) {
                        Log?.Info($"Listing directory : {validDir.PrettyQuote()}");
                        var regexCorrespondingToPath = GetIncludeRegex()[i];
                        foreach (var file in new SourceFilesLister(validDir, CancelToken) { SourcePathFilter = this, Log = Log } .GetFileList()) {
                            if (regexCorrespondingToPath.IsMatch(file.FilePath)) {
                                output.TryAdd(file);
                            }
                        }
                    } else {
                        AddExecutionWarning(new TaskExecutionException(this, $"The property {GetType().GetXmlName(nameof(Include))} part {i} does not designate a file (e.g. /dir/file.ext) nor does it allow to find a base directory to list (e.g. /dir/**), the path in error is : {path}"));
                    }
                }
                i++;
            }

            return output;
        }
        
        /// <inheritdoc cref="OeTask.ExecuteInternal"/>
        protected sealed override void ExecuteInternal() {
            if (this is IOeTaskCompile thisOeTaskCompile) {
                try {
                    Log?.Debug("Is a compile task");
                    var compiledFiles = thisOeTaskCompile.GetCompiledFiles();
                    if (compiledFiles == null) {
                        Log?.Debug("Start file compilation");
                        compiledFiles = OeTaskCompile.CompileFiles(thisOeTaskCompile.GetProperties(), _filesToBuild.Select(f => new UoeFileToCompile(f.FilePath) {
                            FileSize = f.Size
                        }), CancelToken, Log);
                        thisOeTaskCompile.SetCompiledFiles(compiledFiles);
                    }
                    
                    Log?.Debug("Switching original source files for rcode files to build");
                    _filesToBuild = OeTaskCompile.SetRcodeFilesAsSourceInsteadOfSourceFiles(_filesToBuild, compiledFiles);
                } catch(Exception e) {
                    throw new TaskExecutionException(this, e.Message, e);
                }
            }
            switch (this) {
                case IOeTaskFileTargetFile oeTaskFileTargetFile:
                    oeTaskFileTargetFile.ExecuteForFilesTargetFiles(_filesToBuild);
                    break;
                case IOeTaskFileTargetArchive oeTaskFileTargetArchive:
                    oeTaskFileTargetArchive.ExecuteForFilesTargetArchives(_filesToBuild);
                    break;
                default:
                    ExecuteForFilesInternal(_filesToBuild);
                    break;
            }
        }

        /// <summary>
        /// Adds all the files to build to the built files list,
        /// this method is executed instead of <see cref="ExecuteInternal"/> when test mode is on
        /// </summary>
        protected override void ExecuteTestModeInternal() {
            _builtFiles = new FileList<OeFileBuilt>();
            foreach (var file in _filesToBuild) {
                var fileBuilt = GetNewFileBuilt(file);
                fileBuilt.Targets = file.GetAllTargets().ToList();
                _builtFiles.Add(fileBuilt);
            }
        }

        /// <summary>
        /// Returns a file built from a file to build, should be called when the action is done for the source file
        /// In case of a compiled file, this also sets output properties to the file built (db references and required files)(
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        protected OeFileBuilt GetNewFileBuilt(OeFile sourceFile) {
            if (this is IOeTaskCompile thisOeTaskCompile) {
                var newFileBuilt = new OeFileBuiltCompiled(sourceFile);
                var compiledFile = thisOeTaskCompile.GetCompiledFiles()?[sourceFile.FilePath];
                if (compiledFile != null) {
                    newFileBuilt.RequiredFiles = compiledFile.RequiredFiles?.ToList();
                    newFileBuilt.RequiredDatabaseReferences = compiledFile.RequiredDatabaseReferences?.Select(OeDatabaseReference.New).ToList();
                }
                return newFileBuilt;
            }
            return new OeFileBuilt(sourceFile);
        }

        /// <summary>
        /// Execute the task for a set of files
        /// </summary>
        /// <remarks>
        /// - The task should create/add a list of files that it builds, list that is returned by <see cref="IOeTaskFileBuilder.GetFilesBuilt"/>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed
        /// - This method can publish warnings using <see cref="OeTask.AddExecutionWarning"/>
        /// </remarks>
        /// <param name="files"></param>
        /// <exception cref="TaskExecutionException"></exception>
        protected virtual void ExecuteForFilesInternal(FileList<OeFile> files) {
            throw new NotImplementedException();
        }        

        /// <summary>
        /// For a given list of files, set the targets that will be needed from this task
        /// </summary>
        /// <param name="files"></param>
        /// <param name="baseTargetDirectory"></param>
        /// <param name="appendMode"></param>
        public void SetTargetForFiles(FileList<OeFile> files, string baseTargetDirectory, bool appendMode = false) {
            var taskIsCompileTask = this is IOeTaskCompile;
            switch (this) {
                case IOeTaskFileTargetFile taskWithTargetFiles:
                    foreach (var file in files) {
                        var newTargets = taskWithTargetFiles.GetTargetsFiles(file.FilePath, baseTargetDirectory);
                        
                        // change the targets extension to .r for compiled files
                        if (taskIsCompileTask && newTargets != null) {
                            foreach (var targetFile in newTargets) {
                                targetFile.TargetFilePath = Path.ChangeExtension(targetFile.TargetFilePath, UoeConstants.ExtR);
                            }
                        }
                        
                        if (appendMode && file.TargetsFiles != null) {
                            if (newTargets != null) {
                                file.TargetsFiles.AddRange(newTargets);
                            }
                        } else {
                            file.TargetsFiles = newTargets;
                        }
                    }
                    break;
                case IOeTaskFileTargetArchive taskWithTargetArchives:
                    foreach (var file in files) {
                        var newTargets = taskWithTargetArchives.GetTargetsArchives(file.FilePath, baseTargetDirectory);
                        
                        // change the targets extension to .r for compiled files
                        if (taskIsCompileTask && newTargets != null) {
                            foreach (var targetArchive in newTargets) {
                                targetArchive.RelativeTargetFilePath = Path.ChangeExtension(targetArchive.RelativeTargetFilePath, UoeConstants.ExtR);
                            }
                        }
                        
                        if (appendMode && file.TargetsArchives != null) {
                            if (newTargets != null) {
                                file.TargetsArchives.AddRange(newTargets);
                            }
                        } else {
                            file.TargetsArchives = newTargets;
                        }
                    }
                    break;
            }
        }

    }
}