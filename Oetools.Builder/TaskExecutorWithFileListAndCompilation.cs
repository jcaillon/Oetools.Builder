#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutorWithFileListAndCompilation.cs) is part of Oetools.Builder.
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
using System.Runtime.CompilerServices;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;
using Oetools.Utilities.Openedge.Execution.Exceptions;

[assembly: InternalsVisibleTo("Oetools.Builder.Test")]

namespace Oetools.Builder {
       
    public class TaskExecutorWithFileListAndCompilation : TaskExecutorWithFileList {

        public List<UoeCompiledFile> CompiledFiles { get; private set; }
        
        public List<UoeExecutionException> CompilerHandledExceptions { get; private set; }
        
        public int CompilerNumberOfProcessesUsed { get; private set; }
        
        public string SourceDirectory { get; set; }
        
        private UoeExecutionParallelCompile _compiler;

        public override void Execute() {
            Log?.Info("Compiling files from all tasks");
            CompileFiles();
            CheckCompiledFiles();
            try {
                base.Execute();
            } finally {
                _compiler?.Dispose();
            }
        }

        /// <summary>
        /// Compiles all the files that need to be compile for all the <see cref="IOeTaskCompile"/> tasks in <see cref="TaskExecutor.Tasks"/>
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TaskExecutorException"></exception>
        private void CompileFiles() {
            var tryToOptimizeCompilationFolder = Properties?.CompilationOptions?.TryToOptimizeCompilationDirectory ?? OeCompilationOptions.GetDefaultTryToOptimizeCompilationDirectory();
            var filesToCompile = tryToOptimizeCompilationFolder ? GetFilesToCompile(Tasks, TaskFiles, BaseTargetDirectory) :GetFilesToCompile(Tasks, TaskFiles);
            
            if (filesToCompile.Count == 0) {
                return;
            }
            if (Properties == null) {
                throw new ArgumentNullException(nameof(Properties));
            }
            if (Env == null) {
                throw new ArgumentNullException(nameof(Env));
            }
            if (string.IsNullOrEmpty(SourceDirectory)) {
                throw new ArgumentNullException(nameof(SourceDirectory));
            }
            
            _compiler = Properties.GetParallelCompiler(Env, SourceDirectory);
            _compiler.FilesToCompile = filesToCompile;

            CancelSource?.Token.Register(OnExecutionCancel);
            _compiler.Start();
            _compiler.WaitForExecutionEnd();
            
            CompiledFiles = _compiler.CompiledFiles;
            CompilerNumberOfProcessesUsed = _compiler.TotalNumberOfProcesses;

            if (_compiler.ExecutionFailed || _compiler.ExecutionHandledExceptions) {
                CompilerHandledExceptions = _compiler.HandledExceptions;
                if (_compiler.ExecutionFailed || ThrowIfWarning) {
                    throw new TaskExecutorException(this, $"The compiler threw exceptions :\n- {string.Join("\n- ", _compiler.HandledExceptions?.Select(e => e.Message) ?? new List<string>())}");
                }
            }
        }

        /// <summary>
        /// Checks that all the files compiled correctly, can throw an exception if not
        /// </summary>
        /// <exception cref="TaskExecutorException"></exception>
        private void CheckCompiledFiles() {
            if (CompiledFiles == null) {
                return;
            }
            
            foreach (var file in CompiledFiles) {
                if (file.CompiledCorrectly) {
                    continue;     
                }
                if (!ThrowIfWarning && file.CompiledWithWarnings) {
                    continue;
                }
                var stopBuildOnCompilationError = Properties?.BuildOptions?.StopBuildOnCompilationError ?? OeBuildOptions.GetDefaultStopBuildOnCompilationError();
                if (!stopBuildOnCompilationError) {
                    continue;
                }
                throw new TaskExecutorException(this, $"The source file {file.SourceFilePath.PrettyQuote()} was not compiled correctly");
            }
        }
        
        private void OnExecutionCancel() {
            Log?.Debug("Cancel request, killing compilation processes");
            _compiler?.KillProcess();
        }

        /// <summary>
        /// Returns a list of all files that need to be compiled for all the <param name="tasks" /> considering an initial list of
        /// elligible <param name="files" /> (the input files must be compilable file type)
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        internal static List<UoeFileToCompile> GetFilesToCompile(IEnumerable<IOeTask> tasks, IEnumerable<OeFile> files) {
            var compileTasks = tasks.Where(t => t is IOeTaskCompile && t is IOeTaskFilter).Cast<IOeTaskFilter>().ToList();
            return files
                .Where(f => compileTasks.Any(t => t.IsFilePassingFilter(f.SourceFilePath)))
                .Select(f => new UoeFileToCompile(f.SourceFilePath) { FileSize = f.Size })
                .ToList();
        }

        /// <summary>
        /// Returns a list of all files that need to be compiled for all the <param name="tasks" /> considering an initial list of
        /// elligible <param name="files" /> (the input files must be compilable file type)
        /// </summary>
        /// <remarks>
        /// The main thing that makes this method "complicated" is that we try to an appropriate UoeFileToCompile.PreferedTargetDirectory
        /// consider that the source files are on C:\ and that our output directory is on D:\, we can either :
        /// - compile directly on D:\
        /// - compile in temp dir then move to D:\
        /// but a lot of times, the 1st solution is much faster. This is what we do here...
        /// </remarks>
        /// <param name="tasks"></param>
        /// <param name="files"></param>
        /// <param name="baseTargetDirectory"></param>
        /// <returns></returns>
        internal static List<UoeFileToCompile> GetFilesToCompile(IEnumerable<IOeTask> tasks, IEnumerable<OeFile> files, string baseTargetDirectory) {
            var filesToCompile = new List<UoeFileToCompile>();
            
            // list all the tasks that need to compile files
            var compileTasks = tasks.Where(t => t is IOeTaskCompile && t is IOeTaskFilter).Cast<IOeTaskFilter>().ToList();
            
            foreach (var file in files) {
                
                // get all the compile tasks that handle this file
                var compileTasksForThisFile = compileTasks.Where(t => t.IsFilePassingFilter(file.SourceFilePath)).ToList();

                // set all the targets (from all compile tasks) for this file, we need this to set UoeFileToCompile.PreferedTargetDirectory
                foreach (var task in compileTasksForThisFile) {
                    if (task is IOeTaskFileTargetFile taskWithTargetFiles) {
                        (file.TargetsFiles ?? (file.TargetsFiles = new List<OeTargetFile>())).AddRange(taskWithTargetFiles.GetFileTargets(file.SourceFilePath, baseTargetDirectory));
                    }

                    if (task is IOeTaskFileTargetArchive taskWithTargetArchives) {
                        (file.TargetsArchives ?? (file.TargetsArchives = new List<OeTargetArchive>())).AddRange(taskWithTargetArchives.GetFileTargets(file.SourceFilePath, baseTargetDirectory));
                    }
                }

                if (compileTasksForThisFile.Count > 0) {

                    string preferedTargetDirectory = null;
                    var allTargets = file.TargetsArchives?.Select(a => a.TargetPackFilePath).UnionHandleNull(file.TargetsFiles?.Select(f => f.TargetFilePath));
                    var firstTargetWithDifferentDiscDrive = allTargets?.FirstOrDefault(s => !Utils.ArePathOnSameDrive(s, file.SourceFilePath));
                    if (firstTargetWithDifferentDiscDrive != null) {
                        // basically, we found 1 file that targets a different disc drive
                        if (allTargets.All(s => Utils.ArePathOnSameDrive(s, firstTargetWithDifferentDiscDrive))) {
                            // and all targets are on the same disc drive, it is worth targetting this
                            // TODO : maybe the first won't do, search for one that does
                            if (Path.GetFileNameWithoutExtension(firstTargetWithDifferentDiscDrive).Equals(Path.GetFileNameWithoutExtension(file.SourceFilePath))) {
                                preferedTargetDirectory = Path.GetDirectoryName(firstTargetWithDifferentDiscDrive);
                            }
                        }
                    }
                    
                    // at least one compile task takes care of this file, we need to compile it
                    filesToCompile.Add(new UoeFileToCompile(file.SourceFilePath) {
                        FileSize = file.Size,
                        PreferedTargetDirectory = preferedTargetDirectory
                    });
                }
            }

            return filesToCompile;
        }

        protected override IEnumerable<IOeFileToBuildTargetFile> GetFilesReadyForTaskExecution(IOeTaskFile task, List<OeFile> initialFiles) {
            if (task is IOeTaskCompile) {
                var i = 0;
                while (i < initialFiles.Count) {
                    var file = initialFiles[i];
                    var compiledFile = CompiledFiles?.FirstOrDefault(cf => cf.SourceFilePath.Equals(file.SourceFilePath));
                    if (compiledFile != null && (compiledFile.CompiledCorrectly || compiledFile.CompiledWithWarnings)) {
                        file.SourcePathForTaskExecution = compiledFile.CompilationRcodeFilePath;
                    } else {
                        // the file didn't compile, we delete it from the list
                        initialFiles.RemoveAt(i);
                        continue;
                    }
                    i++;
                }
            }
            return base.GetFilesReadyForTaskExecution(task, initialFiles);
        }
    }
}