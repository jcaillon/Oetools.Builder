#region header
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
using System.Collections.Generic;
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
        
        /// <summary>
        /// Validates that the task is correct (correct parameters and can execute)
        /// </summary>
        /// <exception cref="TaskValidationException"></exception>
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
        
        /// <summary>
        /// Given the inclusion wildcard paths and exclusion patterns, returns a list of files on which to apply this task
        /// </summary>
        /// <returns></returns>
        public List<OeFile> GetIncludedFiles() {
            var output = new List<OeFile>();
            var i = 0;
            foreach (var path in GetIncludeStrings()) {
                if (File.Exists(path)) {
                    // the include directly designate a file
                    if (!IsFileExcluded(path)) {
                        output.Add(new OeFile { SourceFilePath = Path.GetFullPath(path) });
                    }
                } else {
                    // the include is a wildcard path, we try to get the "root" folder to list to get all the files
                    var validDir = Utils.GetLongestValidDirectory(path);
                    if (!string.IsNullOrEmpty(validDir)) {
                        Log?.Info($"Listing directory : {validDir.PrettyQuote()}");
                        output.AddRange(new SourceFilesLister(validDir, CancelSource) {
                                SourcePathFilter = this,
                                Log = Log
                            }
                            .GetFileList()
                            .Where(f => GetIncludeRegex()[i].IsMatch(f.SourceFilePath))
                        );
                    } else {
                        AddExecutionWarning(new TaskExecutionException(this, $"The property {GetType().GetXmlName(nameof(Include))} part {i} does not designate a file (e.g. /dir/file.ext) nor does it allow to find a base directory to list (e.g. /dir/**), the path in error is : {path}"));
                    }
                }
                i++;
            }

            // make sure to return unique files
            return output.DistinctBy(file => file.SourceFilePath, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        
        /// <summary>
        /// Main entry for tasks that do operation on files
        /// </summary>
        /// <param name="files"></param>
        /// <exception cref="TaskExecutionException"></exception>
        public void ExecuteForFiles(FileList<OeFile> files) {
            Log?.Debug($"Executing {this}");
            try {
                if (!TestMode) {
                    if (this is IOeTaskCompile thisOeTaskCompile) {
                        Log?.Debug("Is a compile task");
                        var compiledFiles = thisOeTaskCompile.GetCompiledFiles();
                        if (compiledFiles == null) {
                            Log?.Debug("Start file compilation");
                            compiledFiles = OeTaskCompile.CompileFiles(thisOeTaskCompile.GetProperties(), files.Select(f => new UoeFileToCompile(f.SourceFilePath) {
                                FileSize = f.Size
                            }), CancelSource);
                            thisOeTaskCompile.SetCompiledFiles(compiledFiles);
                        }
                        Log?.Debug("Switching orignal source files for rcode files to build");
                        files = OeTaskCompile.SetRcodeFilesAsTargetsInsteadOfSourceFiles(files, compiledFiles);
                    }
                    switch (this) {
                        case IOeTaskFileTargetFile oeTaskFileTargetFile:
                            oeTaskFileTargetFile.ExecuteForFilesTargetFiles(files);
                            break;
                        case IOeTaskFileTargetArchive oeTaskFileTargetArchive:
                            oeTaskFileTargetArchive.ExecuteForFilesTargetArchives(files);
                            break;
                        default:
                            ExecuteForFilesInternal(files);
                            break;
                    }
                } else {
                    _builtFiles = new List<OeFileBuilt>();
                    var isThisTaskCompile = this is IOeTaskCompile;
                    foreach (var file in files) {
                        if (isThisTaskCompile) {
                            // change target file extensions to .r
                            foreach (var target in file.GetAllTargets()) {
                                if (target is OeTargetFile targetFile) {
                                    targetFile.TargetFilePath = Path.ChangeExtension(targetFile.TargetFilePath, UoeConstants.ExtR);
                                } else if (target is OeTargetArchive targetArchive) {
                                    targetArchive.RelativeTargetFilePath = Path.ChangeExtension(targetArchive.RelativeTargetFilePath, UoeConstants.ExtR);
                                }
                            }
                        }
                        var fileBuilt = GetNewFileBuilt(file);
                        fileBuilt.Targets = file.GetAllTargets().ToList();
                        _builtFiles.Add(fileBuilt);
                    }
                }
            } catch (OperationCanceledException) {
                throw;
            } catch (TaskExecutionException) {
                throw;
            } catch (Exception e) {
                AddExecutionErrorAndThrow(new TaskExecutionException(this, $"Unexpected error : {e.Message}", e));
            }
        }

        protected OeFileBuilt GetNewFileBuilt(OeFile sourceFile) {
            if (this is IOeTaskCompile thisOeTaskCompile) {
                var newFileBuilt = new OeFileBuiltCompiled(sourceFile);
                var compiledFile = thisOeTaskCompile.GetCompiledFiles().FirstOrDefault(cf => cf.SourceFilePath.Equals(sourceFile.SourceFilePath));
                if (compiledFile != null) {
                    newFileBuilt.RequiredFiles = compiledFile.RequiredFiles?.ToList();
                    newFileBuilt.RequiredDatabaseReferences = compiledFile.RequiredDatabaseReferences?.Select(OeDatabaseReference.New).ToList();
                }
                return newFileBuilt;
            }
            return new OeFileBuilt(sourceFile);
        }

        /// <inheritdoc cref="IOeTaskFile.ExecuteForFiles"/>
        protected virtual void ExecuteForFilesInternal(List<OeFile> files) {
            throw new NotImplementedException();
        }

        protected sealed override void ExecuteInternal() {
            throw new Exception($"This should never get called, class implementing {nameof(IOeTaskFile)} are using {nameof(ExecuteForFilesInternal)} instead");
        }

        /// <summary>
        /// Override this method to return the list of built files
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<OeFileBuilt> GetFilesBuilt() => _builtFiles;
        
        private List<OeFileBuilt> _builtFiles = null;
    }
}