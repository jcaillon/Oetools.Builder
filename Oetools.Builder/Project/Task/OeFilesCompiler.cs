﻿#region header
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
using System.Linq;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge.Execution;
using Oetools.Utilities.Openedge.Execution.Exceptions;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// This class allows to compile files.
    /// </summary>
    public static class OeFilesCompiler {

        /// <summary>
        /// Sets <see cref="OeFile._sourcePathForTaskExecution"/> to equal to <see cref="UoeCompiledFile.CompilationRcodeFilePath"/>.
        /// This allows the rcode to be handled by a task instead of the original source path.
        /// </summary>
        /// <param name="originalPaths"></param>
        /// <param name="compiledPaths"></param>
        /// <returns></returns>
        public static PathList<OeFile> SetRcodeFilesAsSourceInsteadOfSourceFiles(PathList<OeFile> originalPaths, PathList<UoeCompiledFile> compiledPaths) {
            foreach (var file in originalPaths.ToList()) {
                var compiledFile = compiledPaths[file.Path];
                if (compiledFile != null && (compiledFile.CompiledCorrectly || compiledFile.CompiledWithWarnings)) {
                    // change the source file to copy from
                    file.SourcePathForTaskExecution = compiledFile.CompilationRcodeFilePath;
                } else {
                    // the file didn't compile, we delete it from the list
                    originalPaths.Remove(file);
                }
            }
            return originalPaths;
        }

        /// <summary>
        /// Compile a list of files, handling cancellation and log progression.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="paths"></param>
        /// <param name="cancelToken"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="CompilerException"></exception>
        public static PathList<UoeCompiledFile> CompileFiles(OeProperties properties, PathList<UoeFileToCompile> paths, CancellationToken? cancelToken, ILogger log) {
            if (paths == null || paths.Count == 0) {
                return new PathList<UoeCompiledFile>();
            }
            if (properties == null) {
                throw new ArgumentNullException(nameof(properties));
            }
            using (var compiler = properties.GetParallelCompiler(properties.BuildOptions?.SourceDirectoryPath)) {
                compiler.FilesToCompile = paths;
                log?.ReportProgress(paths.Count, 0, $"Compiling {paths.Count} openedge files");
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