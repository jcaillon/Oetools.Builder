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
using System.Configuration;
using System.IO;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
       
    public class TaskExecutorWithFileListAndCompilation : TaskExecutorWithFileList {

        public List<UoeCompiledFile> CompiledFiles { get; set; }
        
        private UoeExecutionParallelCompile _compiler;

        public override void Execute() {
            Log?.Info("Compiling files from all tasks");
            CompileFiles();
            using (_compiler) {
                base.Execute();
            }
        }

        private void CompileFiles() {
            _compiler = ProjectProperties.GetPar(Env, SourceDirectory);
            _compiler.FilesToCompile = GetFilesToCompile();
            
            // TODO : get all the files that pass the filter of any IOeTaskCompile tasks and compile them
            
            
            throw new NotImplementedException();
        }

        private List<UoeFileToCompile> GetFilesToCompile() {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IOeFileToBuildTargetFile> GetFilesReadyForTaskExecution(IOeTaskFile task, List<OeFile> initialFiles) {
            if (task is IOeTaskCompile) {
                var i = 0;
                while (i <= initialFiles.Count) {
                    var file = initialFiles[i];
                    var compiledFile = CompiledFiles.FirstOrDefault(cf => cf.SourceFilePath.Equals(file.SourceFilePath));
                    if (compiledFile != null && compiledFile.CompiledCorrectly) {
                        file.SourcePathForTaskExecution = compiledFile.CompilationRcodeFilePath;
                    } else {
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