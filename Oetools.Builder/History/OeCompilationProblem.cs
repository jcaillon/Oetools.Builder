using System.Xml.Serialization;

namespace Oetools.Builder.History {
    public abstract class OeCompilationProblem {
            
        /// <summary>
        /// Path of the file in which we found the error
        /// </summary>
        [XmlAttribute(AttributeName ="SourcePath")]
        public string SourcePath { get; set; }
            
        /// <summary>
        /// The path to the file that was compiled to generate this error (you can compile a .p and have the error on a .i)
        /// </summary>
        [XmlAttribute(AttributeName = "CompiledFilePath")]
        public string CompiledFilePath { get; set; }
            
        [XmlAttribute(AttributeName ="Line")]
        public int Line { get; set; }
            
        [XmlAttribute(AttributeName ="Column")]
        public int Column { get; set; }
                        
        /// <summary>
        /// indicates if the error appears several times
        /// </summary>
        [XmlAttribute(AttributeName ="Times")]
        public int Times { get; set; }
            
        [XmlAttribute(AttributeName ="ErrorNumber")]
        public int ErrorNumber { get; set; }
            
        [XmlAttribute(AttributeName ="Message")]
        public string Message { get; set; }
    }
}