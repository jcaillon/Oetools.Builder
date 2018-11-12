using Oetools.Utilities.Lib;

namespace Oetools.Builder.History {
    /// <summary>
    /// Represents a directory.
    /// </summary>
    public interface IOeDirectory : IPathListItem {

        /// <summary>
        /// Path.
        /// </summary>
        new string Path { get; set; }
    }
}