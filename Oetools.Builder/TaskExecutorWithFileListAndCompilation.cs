// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutor.cs) is part of Oetools.Builder.
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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
       
    public class TaskExecutorWithFileListAndCompilation : TaskExecutorWithFileList, IDisposable {

        private OeExecutionParallelCompile _compiler;

        public override void Execute() {
            Log?.Info("Compiling files from all tasks");
            CompileFiles();
            using (_compiler) {
                base.Execute();
            }
        }

        private void CompileFiles() {
            _compiler = new OeExecutionParallelCompile(Env) {
                AnalysisModeSimplifiedDatabaseReferences = ProjectProperties?.CompilationOptions?.UseSimplerAnalysisForDatabaseReference ?? OeCompilationOptions.GetDefaultUseSimplerAnalysisForDatabaseReference(),
                FilesToCompile = GetFilesToCompile(),
                CompileInAnalysisMode = ProjectProperties?.IncrementalBuildOptions?.Disabled ?? OeIncrementalBuildOptions.GetDefaultDisabled(),
                CompileOptions = ProjectProperties?.CompilationOptions?.CompileOptions,
                CompilerMultiCompile = ProjectProperties?.CompilationOptions?.UseCompilerMultiCompile ?? OeCompilationOptions.GetDefaultUseCompilerMultiCompile(),
                CompileStatementExtraOptions = ProjectProperties?.CompilationOptions?.CompileStatementExtraOptions,
                CompileUseXmlXref = ProjectProperties?.CompilationOptions?.CompileWithXmlXref ?? OeCompilationOptions.GetDefaultCompileWithXmlXref(),
                CompileWithDebugList = ProjectProperties?.CompilationOptions?.CompileWithDebugList ?? OeCompilationOptions.GetDefaultCompileWithDebugList(),
                CompileWithListing = ProjectProperties?.CompilationOptions?.CompileWithListing ?? OeCompilationOptions.GetDefaultCompileWithListing(),
                CompileWithPreprocess = ProjectProperties?.CompilationOptions?.CompileWithPreprocess ?? OeCompilationOptions.GetDefaultCompileWithPreprocess(),
                CompileWithXref = ProjectProperties?.CompilationOptions?.CompileWithXref ?? OeCompilationOptions.GetDefaultCompileWithXref(),
                MaxNumberOfProcesses = Math.Max(1, Environment.ProcessorCount * ProjectProperties?.CompilationOptions?.CompileNumberProcessPerCore ?? OeCompilationOptions.GetDefaultCompileNumberProcessPerCore()),
                MinimumNumberOfFilesPerProcess = ProjectProperties?.CompilationOptions?.CompileMinimumNumberOfFilesPerProcess ?? OeCompilationOptions.GetDefaultCompileMinimumNumberOfFilesPerProcess(),
                WorkingDirectory = SourceDirectory,
                NeedDatabaseConnection = true
            };
            
            
            // TODO : get all the files that pass the filter of any ITaskCompile tasks and compile them
            throw new NotImplementedException();
        }

        private List<FileToCompile> GetFilesToCompile() {
            throw new NotImplementedException();
        }

        protected override void ExecuteTaskOnFiles(OeTaskOnFiles task, List<OeFile> files) {
            if (task is ITaskCompile taskCompile) {
                // TODO : instead of executing on sourcepath, execute on the rcode (SourcePathForTaskExecution)
                
                return;
            }
            base.ExecuteTaskOnFiles(task, files);
        }
        
        public void Dispose() {
              
        }
        
    }
}