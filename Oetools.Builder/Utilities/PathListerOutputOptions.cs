using System;
using Oetools.Builder.History;

namespace Oetools.Builder.Utilities {
    public class PathListerOutputOptions {

        /// <summary>
        /// If true, we consider that 2 files are different if they have different hash results.
        /// This implies that we must compute file <see cref="OeFile.Checksum"/> .
        /// </summary>
        /// <remarks>
        /// by default, we consider the file size to see if they are different
        /// </remarks>
        public bool UseCheckSumComparison { get; set; }

        /// <summary>
        /// if true, we consider that 2 files are different if they have different <see cref="OeFile.LastWriteTime"/>.
        /// </summary>
        /// <remarks>
        /// by default, we consider the file size to see if they are different.
        /// </remarks>
        public bool UseLastWriteDateComparison { get; set; } = true;

        /// <summary>
        /// Should return the image of the file as it was previously.
        /// This will be used to compute the state of a file.
        /// And, for instance, to know if a file has been modified since the last time.
        /// </summary>
        public Func<string, IOeFile> GetPreviousFileImage { get; set; }
        
    }
}