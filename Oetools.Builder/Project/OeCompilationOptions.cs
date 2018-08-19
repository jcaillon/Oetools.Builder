using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeCompilationOptions {

        [XmlElement(ElementName = "CompileWithDebugList")]
        public bool? CompileWithDebugList { get; set; }
        internal static bool GetDefaultCompileWithDebugList() => false;

        [XmlElement(ElementName = "CompileWithXref")]
        public bool? CompileWithXref { get; set; }
        internal static bool GetDefaultCompileWithXref() => false;

        [XmlElement(ElementName = "CompileWithListing")]
        public bool? CompileWithListing { get; set; }
        internal static bool GetDefaultCompileWithListing() => false;

        [XmlElement(ElementName = "CompileWithPreprocess")]
        public bool? CompileWithPreprocess { get; set; }
        internal static bool GetDefaultCompileWithPreprocess() => false;

        [XmlElement(ElementName = "UseCompilerMultiCompile")]
        public bool? UseCompilerMultiCompile { get; set; }
        internal static bool GetDefaultUseCompilerMultiCompile() => false;

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
        internal static bool GetDefaultCompileForceUsageOfTemporaryDirectory() => false;

        [XmlElement(ElementName = "CompilableFilePattern")]
        public string CompilableFilePattern { get; set; }
        internal static string GetDefaultCompilableFilePattern() => "*.p;*.cls;*.w;*.t";
                
        [XmlElement(ElementName = "CompileForceSingleProcess")]
        public bool? CompileForceSingleProcess { get; set; }
        internal static bool GetDefaultCompileForceSingleProcess() => false;

        [XmlElement(ElementName = "CompileNumberProcessPerCore")]
        public byte? CompileNumberProcessPerCore { get; set; }
        internal static byte GetDefaultCompileNumberProcessPerCore() => 1;

        [XmlElement(ElementName = "CompileMinimumNumberOfFilesPerProcess")]
        public int? CompileMinimumNumberOfFilesPerProcess { get; set; }
        internal static int GetDefaultCompileMinimumNumberOfFilesPerProcess() => 10;
            
        [XmlElement(ElementName = "UseSimplerAnalysisForDatabaseReference")]
        public bool? UseSimplerAnalysisForDatabaseReference { get; set; }
        internal static bool GetDefaultUseSimplerAnalysisForDatabaseReference() => false;
    }
}