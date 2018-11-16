#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeCompilationOptions.cs) is part of Oetools.Builder.
// 
// Oetools.Builder is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// The options to use to compile your application.
    /// </summary>
    [Serializable]
    public class OeCompilationOptions {

        /// <summary>
        /// Use the DEBUG-LIST option in the COMPILE statement.
        /// </summary>
        /// <remarks>
        /// If true, the compilation process will generate a debugfile (.dbg) that consists of a line-numbered listing of the procedure with the text of all preprocessor include files, names, and parameters inserted.
        /// </remarks>
        [XmlElement(ElementName = "CompileWithDebugList")]
        public bool? CompileWithDebugList { get; set; }
        public static bool GetDefaultCompileWithDebugList() => false;

        /// <summary>
        /// Use the XREF option in the COMPILE statement.
        /// </summary>
        /// <remarks>
        /// If true, the compilation process will generate an xref file (.xrf) that contains reference information on ABL elements, cross-references between procedures and ABL objects, and cross-references between class or interface definition files and ABL objects.
        /// </remarks>
        [XmlElement(ElementName = "CompileWithXref")]
        public bool? CompileWithXref { get; set; }
        public static bool GetDefaultCompileWithXref() => false;

        /// <summary>
        /// Use the XREF-XML option in the COMPILE statement.
        /// This option is only available since openedge 10.2.
        /// </summary>
        /// <remarks>
        /// If true, the compilation process will generate an xref file (.xrf.xml) in XML format that contains reference information on ABL elements, cross-references between procedures and ABL objects, and cross-references between class or interface definition files and ABL objects.
        /// This option cannot be used simultaneously with the XREF option.
        /// </remarks>
        [XmlElement(ElementName = "CompileWithXmlXref")]
        public bool? CompileWithXmlXref { get; set; }
        public static bool GetDefaultCompileWithXmlXref() => false;

        /// <summary>
        /// Use the LISTING option in the COMPILE statement.
        /// </summary>
        /// <remarks>
        /// If true, the compilation process will produce a file (.lis) including:
        /// - The name of the file containing the procedure or class you compile 
        /// - The date and time at the start of the compilation 
        /// - The number of each line in the procedure or class file 
        /// - The block number where each statement belongs 
        /// - The complete text of all include files (except encrypted include files) and the names of any sub-procedures and user-defined functions 
        /// </remarks>
        [XmlElement(ElementName = "CompileWithListing")]
        public bool? CompileWithListing { get; set; }
        public static bool GetDefaultCompileWithListing() => false;

        /// <summary>
        /// Use the PREPROCESS option in the COMPILE statement.
        /// </summary>
        /// <remarks>
        /// If true, the compilation process will generate a file (.preprocessed) that contains a final version of your source code after all include files have been inserted and all text substitutions have been performed.
        /// </remarks>
        [XmlElement(ElementName = "CompileWithPreprocess")]
        public bool? CompileWithPreprocess { get; set; }
        public static bool GetDefaultCompileWithPreprocess() => false;
        
        /// <summary>
        /// Use the MULTI-COMPILE option in the COMPILE statement.
        /// This option is only available since openedge 10.2.
        /// </summary>
        /// <remarks>
        /// When set to TRUE, ABL compiles only those class definition files in the inherited class hierarchy that are not found in the cache. ABL also caches any classes or interfaces it compiles to avoid recompiling them during the session.
        /// When set to FALSE, ABL compiles all class definition files in the inherited class hierarchy. ABL also clears the cache of any classes or interfaces compiled during the session. The default value is FALSE.
        /// </remarks>
        [XmlElement(ElementName = "UseCompilerMultiCompile")]
        public bool? UseCompilerMultiCompile { get; set; }
        public static bool GetDefaultUseCompilerMultiCompile() => false;

        /// <summary>
        /// Control the behavior of the openedge compiler, allowing to compile with different options.
        /// This option is only available since openedge 11.7.
        /// </summary>
        /// <remarks>
        /// The options are set and stored as a comma-separated list.
        /// The acceptable values in openedge 11.7 are:
        /// - require-full-names
        /// - require-field-qualifiers
        /// - require-full-keywords.
        /// </remarks>
        [XmlElement(ElementName = "CompileOptions")]
        public string CompileOptions { get; set; }
        
        /// <summary>
        /// Sets extra COMPILE options to use in the COMPILE statement.
        /// </summary>
        /// <remarks>
        /// Use this property to specify a COMPILE option that is not managed by this tool.
        /// </remarks>
        /// <example>
        /// MIN-SIZE = TRUE
        /// XCODE = "progress"
        /// GENERATE-MD5 = TRUE
        /// LANGUAGES (French-Canadian:French:English,Portuguese:Spanish,New-York:American:English)
        /// </example>
        [XmlElement(ElementName = "CompileStatementExtraOptions")]
        public string CompileStatementExtraOptions { get; set; }

        /// <summary>
        /// Try to find the best location to compile an rcode instead of using the default temporary directory.
        /// </summary>
        /// <remarks>
        /// By default, all rcode generated are saved in the temporary directory and then moved to the different target locations. This might result in unnecessary delays and can be avoided by saving the rcode directly to the target location. This option allows the tool to make this small simplification when it makes sense.
        /// </remarks>
        [XmlElement(ElementName = "TryToOptimizeCompilationDirectory")]
        public bool? TryToOptimizeCompilationDirectory { get; set; }
        public static bool GetDefaultTryToOptimizeCompilationDirectory() => true;

        /// <summary>
        /// A comma-separated list of file extension patterns that represent ABL compilable files.
        /// </summary>
        /// <remarks>
        /// This is a default filter that is used for each "compile" task and which allows to process only certain types of files. 
        /// </remarks>
        [XmlElement(ElementName = "CompilableFileExtensionPattern")]
        public string CompilableFileExtensionPattern { get; set; }
        public static string GetDefaultCompilableFileExtensionPattern() => OeBuilderConstants.CompilableExtensionsPattern;
                
        /// <summary>
        /// The number of openedge process to start simultaneously per core (on your computer) in order to compile your application.
        /// </summary>
        /// <remarks>
        /// To speed up the compilation process, this tool can start parallel openedge processes to compile your application.
        /// This option allows to fine tune the number of processes to start.
        /// </remarks>
        [XmlElement(ElementName = "NumberProcessPerCore")]
        public byte? NumberProcessPerCore { get; set; }
        public static byte GetDefaultNumberProcessPerCore() => 1;
        
        /// <summary>
        /// Use a single process to compile your application.
        /// </summary>
        /// <remarks>
        /// This option overload the default behavior of this tool, that aims to use parallel processes to speed up the compilation process.
        /// This can be useful for low end machines or when connecting to a database in mono-user (-1 parameter).
        /// </remarks>
        [XmlElement(ElementName = "ForceSingleProcess")]
        public bool? ForceSingleProcess { get; set; }
        public static bool GetDefaultForceSingleProcess() => false;
        
        public static int GetNumberOfProcessesToUse(OeCompilationOptions compilationOptions) => compilationOptions?.ForceSingleProcess ?? GetDefaultForceSingleProcess() ? 1 : Math.Max(1, Environment.ProcessorCount * (compilationOptions?.NumberProcessPerCore ?? GetDefaultNumberProcessPerCore()));

        /// <summary>
        /// The minimum number of files that needs to be allocated to one of the processes before justifying the use of a new process.
        /// </summary>
        /// <remarks>
        /// This option exists because there would be an important overhead to start 2 openedge processes to compile only 2 files.
        /// </remarks>
        [XmlElement(ElementName = "MinimumNumberOfFilesPerProcess")]
        public int? MinimumNumberOfFilesPerProcess { get; set; }
        public static int GetDefaultMinimumNumberOfFilesPerProcess() => 10;
    }
}