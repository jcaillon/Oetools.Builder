using System;

namespace Oetools.Builder.Utilities {
    
    /// <summary>
    /// A debug/trace logger.
    /// </summary>
    public interface ITraceLogger {
        
        /// <summary>
        /// Write the message to the debug log.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        void Write(string message, Exception e = null);
        
        /// <summary>
        /// Report progress.
        /// </summary>
        /// <param name="max"></param>
        /// <param name="current"></param>
        /// <param name="message"></param>
        void ReportProgress(int max, int current, string message);
    }
}