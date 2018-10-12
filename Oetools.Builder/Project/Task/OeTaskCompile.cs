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
using System.IO;
using System.Linq;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;
using Oetools.Utilities.Openedge.Execution.Exceptions;

namespace Oetools.Builder.Project.Task {
    
    public static class OeTaskCompile {

        public static FileList<OeFile> SetRcodeFilesAsSourceInsteadOfSourceFiles(FileList<OeFile> originalFiles, FileList<UoeCompiledFile> compiledFiles) {
            foreach (var file in originalFiles.ToList()) {
                var compiledFile = compiledFiles[file.FilePath];
                if (compiledFile != null && (compiledFile.CompiledCorrectly || compiledFile.CompiledWithWarnings)) {
                    // change the source file to copy from
                    file.SourcePathForTaskExecution = compiledFile.CompilationRcodeFilePath;
                } else {
                    // the file didn't compile, we delete it from the list
                    originalFiles.Remove(file);
                }
            }
            return originalFiles;
        }

        public static FileList<UoeCompiledFile> CompileFiles(OeProperties properties, FileList<UoeFileToCompile> files, CancellationToken? cancelToken, ILogger log) {
            if (files == null || files.Count == 0) {
                return new FileList<UoeCompiledFile>();
            }
            if (properties == null) {
                throw new ArgumentNullException(nameof(properties));
            }
            using (var compiler = properties.GetParallelCompiler(properties.BuildOptions?.SourceDirectoryPath)) {
                compiler.FilesToCompile = files;
                log?.ReportProgress(files.Count, 0, $"Compiling {files.Count} openedge files");
                compiler.Start();
                bool exited;
                do {
                    exited = compiler.WaitForExecutionEnd(1000, cancelToken);
                    int nbDone = compiler.NumberOfFilesTreated;
                    log?.ReportProgress(compiler.NumberOfFilesToCompile, nbDone, $"Compiling openedge files {nbDone}/{compiler.NumberOfFilesToCompile} ({compiler.NumberOfProcessesRunning} process running)");
                } while (!exited && !(cancelToken?.IsCancellationRequested ?? false));
                if (cancelToken?.IsCancellationRequested ?? false) {
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