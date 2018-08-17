using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeCompilationOptions {

        [XmlElement(ElementName = "CompileWithDebugList")]
        public bool? CompileWithDebugList { get; set; }

        [XmlElement(ElementName = "CompileWithXref")]
        public bool? CompileWithXref { get; set; }

        [XmlElement(ElementName = "CompileWithListing")]
        public bool? CompileWithListing { get; set; }

        [XmlElement(ElementName = "CompileWithPreprocess")]
        public bool? CompileWithPreprocess { get; set; }

        [XmlElement(ElementName = "UseCompilerMultiCompile")]
        public bool? UseCompilerMultiCompile { get; set; }

        /// <summary>
        /// only since 11.7 : require-full-names, require-field-qualifiers, require-full-keywords
        /// </summary>
        [XmlElement(ElementName = "CompileOptions")]
        public bool? CompileOptions { get; set; }

        /// <summary>
        /// Force the usage of a temporary Directory to compile the .r code files
        /// </summary>
        [XmlElement(ElementName = "CompileForceUsageOfTemporaryDirectory")]
        public bool? CompileForceUsageOfTemporaryDirectory { get; set; }

        [XmlElement(ElementName = "CompilableFilePattern")]
        public string CompilableFilePattern { get; set; }
                
        [XmlElement(ElementName = "CompileForceSingleProcess")]
        public bool? CompileForceSingleProcess { get; set; }

        [XmlElement(ElementName = "CompileNumberProcessPerCore")]
        public byte? CompileNumberProcessPerCore { get; set; }

        [XmlElement(ElementName = "CompileMinimumNumberOfFilesPerProcess")]
        public int? CompileMinimumNumberOfFilesPerProcess { get; set; }
            
        [XmlElement(ElementName = "UseSimplerAnalysisForDatabaseReference")]
        public bool? UseSimplerAnalysisForDatabaseReference { get; set; }
    }
}