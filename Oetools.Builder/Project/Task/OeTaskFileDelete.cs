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
    /// This tasks simply delete files path (in a folder)
    /// </summary>
    [Serializable]
    [XmlRoot("Delete")]
    public class OeTaskFileDelete : OeTaskFile {
        
        protected override void ExecuteForFilesInternal(FileList<OeFile> files) {

            if (files.Count <= 0) {
                return;
            }

            Log?.ReportProgress(files.Count, 0, $"Deleting {files.Count} files");

            var nbDone = 0;
            foreach (var file in files) {
                CancelSource?.Token.ThrowIfCancellationRequested();
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
                Log?.ReportProgress(files.Count, nbDone, $"Deleting files {nbDone}/{files.Count}");
            }
            
        }

        protected override void ExecuteTestModeInternal() {
            // this task doesn't actually build anything, it just deletes files
        }
    }
}