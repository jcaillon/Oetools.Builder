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
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {
    
    public static class OeTaskCompile {

        public static List<UoeCompiledFile> CompileFiles(OeProperties properties, List<UoeCompiledFile> compiledFiles, ref List<OeFile> files) {
            if (compiledFiles == null) {
                throw new NotImplementedException("Implement this case, we need to compile the files first");
            }
            
            var stopBuildOnCompilationError = properties?.BuildOptions?.StopBuildOnCompilationError ?? OeBuildOptions.GetDefaultStopBuildOnCompilationError();
            var stopBuildOnCompilationWarning = properties?.BuildOptions?.StopBuildOnCompilationWarning ?? OeBuildOptions.GetDefaultStopBuildOnCompilationWarning();
            
            foreach (var file in compiledFiles) {
                if (file.CompiledCorrectly) {
                    continue;     
                }
                if (!stopBuildOnCompilationError) {
                    continue;
                }
                if (!stopBuildOnCompilationWarning && file.CompiledWithWarnings) {
                    continue;
                }
                throw new TaskCompileException($"The source file {file.SourceFilePath.PrettyQuote()} was not compiled correctly", file.CompilationErrors);
            }
            
            var i = 0;
            while (i < files.Count) {
                var file = files[i];
                var compiledFile = compiledFiles.FirstOrDefault(cf => cf.SourceFilePath.Equals(file.SourceFilePath));
                if (compiledFile != null && (compiledFile.CompiledCorrectly || compiledFile.CompiledWithWarnings)) {
                    // change the source file to copy from, and change the target extensions
                    file.SourcePathForTaskExecution = compiledFile.CompilationRcodeFilePath;
                    foreach (var targetFile in file.TargetsFiles.ToNonNullList()) {
                        targetFile.TargetFilePath = Path.ChangeExtension(targetFile.TargetFilePath, UoeConstants.ExtR);
                    }
                    foreach (var targetFile in file.TargetsArchives.ToNonNullList()) {
                        targetFile.RelativeTargetFilePath = Path.ChangeExtension(targetFile.RelativeTargetFilePath, UoeConstants.ExtR);
                    }
                } else {
                    // the file didn't compile, we delete it from the list
                    files.RemoveAt(i);
                    continue;
                }
                i++;
            }
            
            return compiledFiles;
        }
    }
}