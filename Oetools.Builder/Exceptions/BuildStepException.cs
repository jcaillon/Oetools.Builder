using System;
using Oetools.Builder.Project;

namespace Oetools.Builder.Exceptions {
    
    public class BuildStepException : BuilderException {
        
        public AOeBuildStep BuildStep { get; }
        
        public string PropertyName { get; set; }
       
        public BuildStepException(AOeBuildStep buildStep, string message) : base(message) {
            BuildStep = buildStep;
        }
        public BuildStepException(AOeBuildStep buildStep, string message, Exception innerException) : base(message, innerException) {
            BuildStep = buildStep;
        }

        public override string Message => $"{(string.IsNullOrEmpty(PropertyName) ? "" : $"{PropertyName}: ")}{BuildStep}: {base.Message ?? ""}";
    }
}