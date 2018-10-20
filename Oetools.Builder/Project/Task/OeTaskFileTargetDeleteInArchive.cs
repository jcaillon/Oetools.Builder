using System;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class that allows to delete files within archives.
    /// </summary>
    public abstract class OeTaskFileTargetDeleteInArchive : OeTaskFileTarget {
        
        /// <summary>
        /// The relative file path pattern to delete inside the matched archive.
        /// </summary>
        /// <remarks>
        /// This string can contain ; to separate several values.
        /// Each value can contain placeholders.
        /// </remarks>
        /// <returns></returns>
        protected abstract string GetRelativeFilePatternToDelete();
        
        /// <summary>
        /// Returns the property name of the property which holds the value <see cref="GetRelativeFilePatternToDelete"/>.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetRelativeFilePatternToDeletePropertyName();

        /// <summary>
        /// Returns an instance of an archiver.
        /// </summary>
        /// <returns></returns>
        protected abstract IArchiver GetArchiver();

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
                        // TODO :dzerfezrf
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

        /// <inheritdoc cref="OeTaskFileTarget.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // this task doesn't actually build anything, it just deletes files
        }
    }
}