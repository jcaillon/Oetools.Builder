using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeCompilationOptions {

        [XmlElement(ElementName = "CompileWithDebugList")]
        public bool? CompileWithDebugList { get; set; }
        internal bool GetDefaultCompileWithDebugList() => false;

        [XmlElement(ElementName = "CompileWithXref")]
        public bool? CompileWithXref { get; set; }
        internal bool GetDefaultCompileWithXref() => false;

        [XmlElement(ElementName = "CompileWithListing")]
        public bool? CompileWithListing { get; set; }
        internal bool GetDefaultCompileWithListing() => false;

        [XmlElement(ElementName = "CompileWithPreprocess")]
        public bool? CompileWithPreprocess { get; set; }
        internal bool GetDefaultCompileWithPreprocess() => false;

        [XmlElement(ElementName = "UseCompilerMultiCompile")]
        public bool? UseCompilerMultiCompile { get; set; }
        internal bool GetDefaultUseCompilerMultiCompile() => false;

        /// <summary>
        /// only since 11.7 : require-full-names, require-field-qualifiers, require-full-keywords
        /// </summary>
        [XmlElement(ElementName = "CompileOptions")]
        public string CompileOptions { get; set; }
        
        [XmlElement(ElementName = "CompileStatementExtraOptions")]
        public string CompileStatementExtraOptions { get; set; }

        /// <summary>
        /// Force the usage of a temporary Directory to compile the .r code files
        /// </summary>
        [XmlElement(ElementName = "CompileForceUsageOfTemporaryDirectory")]
        public bool? CompileForceUsageOfTemporaryDirectory { get; set; }
        internal bool GetDefaultCompileForceUsageOfTemporaryDirectory() => false;

        [XmlElement(ElementName = "CompilableFilePattern")]
        public string CompilableFilePattern { get; set; }
        internal string GetDefaultCompilableFilePattern() => "*.p;*.cls;*.w;*.t";
                
        [XmlElement(ElementName = "CompileForceSingleProcess")]
        public bool? CompileForceSingleProcess { get; set; }
        internal bool GetDefaultCompileForceSingleProcess() => false;

        [XmlElement(ElementName = "CompileNumberProcessPerCore")]
        public byte? CompileNumberProcessPerCore { get; set; }
        internal byte GetDefaultCompileNumberProcessPerCore() => 1;

        [XmlElement(ElementName = "CompileMinimumNumberOfFilesPerProcess")]
        public int? CompileMinimumNumberOfFilesPerProcess { get; set; }
        internal int GetDefaultCompileMinimumNumberOfFilesPerProcess() => 10;
            
        [XmlElement(ElementName = "UseSimplerAnalysisForDatabaseReference")]
        public bool? UseSimplerAnalysisForDatabaseReference { get; set; }
        internal bool GetDefaultUseSimplerAnalysisForDatabaseReference() => false;
    }
}