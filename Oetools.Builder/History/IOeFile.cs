using System;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.History {
    /// <summary>
    /// Represents a file.
    /// </summary>
    public interface IOeFile : IPathListItem {
        
        /// <summary>
        /// Path.
        /// </summary>
        new string Path { get; set; }
        
        /// <summary>
        /// Datetime at which this file was last modified.
        /// </summary>
        DateTime LastWriteTime { get; set; }
        
        /// <summary>
        /// Size of this file.
        /// </summary>
        long Size { get; set; }

        /// <summary>
        /// A checksum value for this file.
        /// </summary>
        string Checksum { get; set; }

        /// <summary>
        /// Represents the state of the file for this build compare to the previous one.
        /// </summary>
        OeFileState State { get; set; }
    }
}