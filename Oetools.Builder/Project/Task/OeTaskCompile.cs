#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskCompile.cs) is part of Oetools.Builder.
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
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;
using Oetools.Utilities.Openedge.Execution.Exceptions;

namespace Oetools.Builder.Project.Task {
    
    public static class OeTaskCompile {

        public static FileList<OeFile> SetRcodeFilesAsTargetsInsteadOfSourceFiles(FileList<OeFile> originalFiles, FileList<UoeCompiledFile> compiledFiles) {
            var filesToRemove = new List<OeFile>();
            foreach (var file in originalFiles) {
                var compiledFile = compiledFiles[file.SourceFilePath];
                if (compiledFile != null && (compiledFile.CompiledCorrectly || compiledFile.CompiledWithWarnings)) {
                    
                    // change the source file to copy from
                    file.SourcePathForTaskExecution = compiledFile.CompilationRcodeFilePath;
                    
                    // change the targets extentions
                    foreach (var targetFile in file.TargetsFiles.ToNonNullList()) {
                        targetFile.TargetFilePath = Path.ChangeExtension(targetFile.TargetFilePath, UoeConstants.ExtR);
                    }
                    foreach (var targetFile in file.TargetsArchives.ToNonNullList()) {
                        targetFile.RelativeTargetFilePath = Path.ChangeExtension(targetFile.RelativeTargetFilePath, UoeConstants.ExtR);
                    }
                } else {
                    // the file didn't compile, we delete it from the list
                    filesToRemove.Add(file);
                }
            }
            foreach (var file in filesToRemove) {
                originalFiles.Remove(file);
            }
            return originalFiles;
        }

        public static FileList<UoeCompiledFile> CompileFiles(OeProperties properties, FileList<UoeFileToCompile> files, CancellationTokenSource cancelSource) {
            if (files == null || files.Count == 0) {
                return new FileList<UoeCompiledFile>();
            }
            if (properties == null) {
                throw new ArgumentNullException(nameof(properties));
            }
            using (var compiler = properties.GetParallelCompiler(properties.BuildOptions?.SourceDirectoryPath)) {
                compiler.FilesToCompile = files;

                compiler.Start();
                compiler.WaitForExecutionEnd(cancelSource: cancelSource);
                if (cancelSource?.IsCancellationRequested ?? false) {
                    compiler.KillProcess();
                    compiler.WaitForExecutionEnd();
                }
                if (compiler.ExecutionFailed || compiler.ExecutionHandledExceptions) {
                    if (compiler.HandledExceptions.Exists(e => e is UoeExecutionCompilationStoppedException) || compiler.ExecutionFailed || (properties.BuildOptions?.TreatWarningsAsErrors ?? OeBuildOptions.GetDefaultTreatWarningsAsErrors())) {
                        throw new CompilerException(compiler.HandledExceptions);
                    }
                }
                return compiler.CompiledFiles;
            }

        }
    }
}