
using System;

namespace Oetools.Builder.Exceptions {
    public class TaskExecutorException : Exception {
        public TaskExecutorException(string message) : base(message) { }
        public TaskExecutorException(string message, Exception innerException) : base(message, innerException) { }
    }
}