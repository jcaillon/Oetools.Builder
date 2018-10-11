using System;

namespace Oetools.Builder.Utilities {
    public interface ITraceLogger {
        void Write(string message, Exception e = null);
        void ReportProgress(int max, int current, string message);
    }
}