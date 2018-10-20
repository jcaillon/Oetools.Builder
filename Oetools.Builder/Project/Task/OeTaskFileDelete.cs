#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileDelete.cs) is part of Oetools.Builder.
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
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// This tasks allows to delete files path.
    /// </summary>
    [Serializable]
    [XmlRoot("Delete")]
    public class OeTaskFileDelete : OeTaskFile {
        
        /// <inheritdoc cref="OeTaskFile.ExecuteForFilesInternal"/>
        protected override void ExecuteForFilesInternal(PathList<OeFile> paths) {

            if (paths.Count <= 0) {
                return;
            }

            Log?.ReportProgress(paths.Count, 0, $"Deleting {paths.Count} files");

            var nbDone = 0;
            foreach (var file in paths) {
                CancelToken?.ThrowIfCancellationRequested();
                if (File.Exists(file.SourcePathForTaskExecution)) {
                    Log?.Trace?.Write($"Deleting file {file.SourcePathForTaskExecution.PrettyQuote()}");
                    try {
                        File.Delete(file.SourcePathForTaskExecution);
                    } catch (Exception e) {
                        throw new TaskExecutionException(this, $"Could not delete file {file.SourcePathForTaskExecution.PrettyQuote()}", e);
                    }
                } else {
                    Log?.Trace?.Write($"Deleting file not existing {file.SourcePathForTaskExecution.PrettyQuote()}");
                }
                nbDone++;
                Log?.ReportProgress(paths.Count, nbDone, $"Deleting files {nbDone}/{paths.Count}");
            }
            
        }

        /// <inheritdoc cref="OeTask.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // this task doesn't actually build anything, it just deletes files
        }
    }
}