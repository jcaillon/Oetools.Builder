using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Oetools.Packager.Core {
    
    /// <summary>
    ///     Errors found for this file, either from compilation or from prolint
    /// </summary>
    [Serializable]
    public class FileError {

        /// <summary>
        ///     The path to the file that was compiled to generate this error (you can compile a .p and have the error on a .i)
        /// </summary>
        public string CompiledFilePath { get; set; }

        /// <summary>
        ///     Path of the file in which we found the error
        /// </summary>
        public string SourcePath { get; set; }

        public ErrorLevel Level { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int ErrorNumber { get; set; }
        public string Message { get; set; }
        public string Help { get; set; }
        public bool FromProlint { get; set; }

        /// <summary>
        ///     indicates if the error appears several times
        /// </summary>
        public int Times { get; set; }

        public FileError Copy() {
            return new FileError {
                CompiledFilePath = CompiledFilePath,
                SourcePath = SourcePath,
                Line = Line,
                Column = Column,
                ErrorNumber = ErrorNumber,
                FromProlint = FromProlint,
                Help = Help,
                Level = Level,
                Message = Message,
                Times = Times
            };
        }
    }

    /// <summary>
    ///     Describes the error level, the num is also used for MARKERS in scintilla
    ///     and thus must start at 0
    /// </summary>
    public enum ErrorLevel {
        [Description("Error(s), good!")] 
        [XmlEnum("0")]
        NoErrors = 0,

        [Description("Info")] 
        [XmlEnum("1")] Information,

        [Description("Warning(s)")] 
        [XmlEnum("2")]
        Warning,

        [Description("Huge warning(s)")] 
        [XmlEnum("3")]
        StrongWarning,

        [Description("Error(s)")] 
        [XmlEnum("4")]
        Error, // while compiling, from this level, the file doesn't compile

        [Description("Critical error(s)!")] 
        [XmlEnum("5")]
        Critical
    }
}