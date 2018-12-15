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
using System.Linq;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Properties;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;
using Oetools.Utilities.Openedge.Execution.Exceptions;

namespace Oetools.Builder.Utilities {
    
    /// <summary>
    /// This class allows to compile files.
    /// </summary>
    public class OeFilesCompiler : IDisposable {

        /// <summary>
        /// Sets <see cref="OeFile._sourcePathForTaskExecution"/> to equal to <see cref="UoeCompiledFile.CompilationRcodeFilePath"/>.
        /// This allows the rcode to be handled by a task instead of the original source path.
        /// </summary>
        /// <param name="originalPaths"></param>
        /// <param name="compiledPaths"></param>
        /// <returns></returns>
        public static PathList<IOeFileToBuild> SetRcodeFilesAsSourceInsteadOfSourceFiles(PathList<IOeFileToBuild> originalPaths, PathList<UoeCompiledFile> compiledPaths) {
            foreach (var file in originalPaths.ToList()) {
                var compiledFile = compiledPaths[file.Path];
                if (compiledFile != null && (compiledFile.CompiledCorrectly || compiledFile.CompiledWithWarnings)) {
                    // change the source file to copy from
                    file.PathForTaskExecution = compiledFile.CompilationRcodeFilePath;
                } else {
                    // the file didn't compile, we remove its targets
                    file.TargetsToBuild = null;
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
        public PathList<UoeCompiledFile> CompileFiles(OeProperties properties, PathList<UoeFileToCompile> paths, CancellationToken? cancelToken, ILogger log) {
            if (paths == null || paths.Count == 0) {
                return new PathList<UoeCompiledFile>();
            }
            if (properties == null) {
                throw new ArgumentNullException(nameof(properties));
            }

            _compiler = properties.GetParallelCompiler((properties.BuildOptions?.SourceDirectoryPath).TakeDefaultIfNeeded(OeBuildOptions.GetDefaultSourceDirectoryPath()));
            _compiler.FilesToCompile = paths;
            log?.Debug($"Compiling {paths.Count} openedge files.");
            log?.ReportProgress(paths.Count, 0, $"Compiling {paths.Count} openedge files.");
            _compiler.Start();
            bool exited;
            do {
                exited = _compiler.WaitForExecutionEnd(500, cancelToken);
                int nbDone = _compiler.NumberOfFilesTreated;
                log?.ReportProgress(_compiler.NumberOfFilesToCompile, nbDone, $"Compiling openedge files {nbDone}/{_compiler.NumberOfFilesToCompile} ({_compiler.NumberOfProcessesRunning} process running).");
            } while (!exited && !(cancelToken?.IsCancellationRequested ?? false));
            if (cancelToken?.IsCancellationRequested ?? false) {
                _compiler.KillProcess();
                _compiler.WaitForExecutionEnd();
            }
            if (_compiler.ExecutionFailed) {
                throw new CompilerException(_compiler.HandledExceptions);
            }
            if (_compiler.ExecutionHandledExceptions) {
                if (_compiler.HandledExceptions.Exists(e => e is UoeExecutionCompilationStoppedException) || (properties.BuildOptions?.StopBuildOnTaskWarning ?? OeBuildOptions.GetDefaultStopBuildOnTaskWarning())) {
                    throw new CompilerException(_compiler.HandledExceptions);
                }
            }
            log?.Info($"Compiled {_compiler.CompiledFiles.Count} openedge files in {_compiler.ExecutionTimeSpan?.ConvertToHumanTime() ?? "?"}.");
            return _compiler.CompiledFiles;
        }

        private UoeExecutionParallelCompile _compiler;

        public void Dispose() {
            _compiler?.Dispose();
        }
    }
}