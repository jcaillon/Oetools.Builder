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
using System.Text.RegularExpressions;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class for tasks that operates on files and that have targets for aforementioned files.
    /// </summary>
    public abstract class OeTaskFileTarget : OeTaskFile, IOeTaskFileTarget {
        
        private PathList<UoeCompiledFile> CompiledPaths { get; set; }
        
        /// <inheritdoc cref="IOeTaskCompile.SetCompiledFiles"/>
        public void SetCompiledFiles(PathList<UoeCompiledFile> compiledPath) => CompiledPaths = compiledPath;
        
        /// <inheritdoc cref="IOeTaskCompile.GetCompiledFiles"/>
        public PathList<UoeCompiledFile> GetCompiledFiles() => CompiledPaths;
        
        protected PathList<OeFileBuilt> _builtPaths;
        
        /// <inheritdoc cref="IOeTaskFileBuilder.GetFilesBuilt"/>
        public PathList<OeFileBuilt> GetFilesBuilt() => _builtPaths;
        
        /// <inheritdoc cref="IOeTaskFileTarget.SetTargetForFiles"/>
        public void SetTargetForFiles(PathList<OeFile> paths, string baseTargetDirectory, bool appendMode = false) {
            var taskIsCompileTask = this is IOeTaskCompile;
            switch (this) {
                case IOeTaskFileTargetFile taskWithTargetFiles:
                    foreach (var file in paths) {
                        var newTargets = taskWithTargetFiles.GetTargetsFiles(file.Path, baseTargetDirectory);
                        
                        // change the targets extension to .r for compiled files.
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
                    foreach (var file in paths) {
                        var newTargets = taskWithTargetArchives.GetTargetsArchives(file.Path, baseTargetDirectory);
                        
                        // change the targets extension to .r for compiled files.
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
        
        /// <summary>
        /// Adds all the files to build to the built files list,
        /// this method is executed instead of <see cref="OeTask.ExecuteInternal"/> when test mode is on.
        /// </summary>
        protected override void ExecuteTestModeInternal() {
            _builtPaths = new PathList<OeFileBuilt>();
            foreach (var file in GetFilesToBuild()) {
                var fileBuilt = GetNewFileBuilt(file);
                fileBuilt.Targets = file.GetAllTargets().ToList();
                _builtPaths.Add(fileBuilt);
            }
        }

        /// <summary>
        /// Returns a file built from a file to build, should be called when the action is done for the source file.
        /// In case of a compiled file, this also sets output properties to the file built (db references and required files).
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        protected OeFileBuilt GetNewFileBuilt(OeFile sourceFile) {
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
        
    }
}