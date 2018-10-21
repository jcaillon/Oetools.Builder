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

namespace Oetools.Builder.Project {
    
    [Serializable]
    public class OeCompilationOptions {

        [XmlElement(ElementName = "CompileWithDebugList")]
        public bool? CompileWithDebugList { get; set; }
        public static bool GetDefaultCompileWithDebugList() => false;

        [XmlElement(ElementName = "CompileWithXmlXref")]
        public bool? CompileWithXmlXref { get; set; }
        public static bool GetDefaultCompileWithXmlXref() => false;

        [XmlElement(ElementName = "CompileWithXref")]
        public bool? CompileWithXref { get; set; }
        public static bool GetDefaultCompileWithXref() => false;

        [XmlElement(ElementName = "CompileWithListing")]
        public bool? CompileWithListing { get; set; }
        public static bool GetDefaultCompileWithListing() => false;

        [XmlElement(ElementName = "CompileWithPreprocess")]
        public bool? CompileWithPreprocess { get; set; }
        public static bool GetDefaultCompileWithPreprocess() => false;

        [XmlElement(ElementName = "UseCompilerMultiCompile")]
        public bool? UseCompilerMultiCompile { get; set; }
        public static bool GetDefaultUseCompilerMultiCompile() => false;

        /// <summary>
        /// only since 11.7 : require-full-names, require-field-qualifiers, require-full-keywords
        /// </summary>
        [XmlElement(ElementName = "CompileOptions")]
        public string CompileOptions { get; set; }
        
        [XmlElement(ElementName = "CompileStatementExtraOptions")]
        public string CompileStatementExtraOptions { get; set; }

        /// <summary>
        /// Do not always use temporary Directory to compile the .r code files
        /// </summary>
        [XmlElement(ElementName = "TryToOptimizeCompilationDirectory")]
        public bool? TryToOptimizeCompilationDirectory { get; set; }
        public static bool GetDefaultTryToOptimizeCompilationDirectory() => true;

        [XmlElement(ElementName = "CompilableFileExtensionPattern")]
        public string CompilableFileExtensionPattern { get; set; }
        public static string GetDefaultCompilableFileExtensionPattern() => "*.p;*.cls;*.w;*.t";
                
        [XmlElement(ElementName = "ForceSingleProcess")]
        public bool? ForceSingleProcess { get; set; }
        public static bool GetDefaultForceSingleProcess() => false;

        public static int GetNumberOfProcessesToUse(OeCompilationOptions compilationOptions) => compilationOptions?.ForceSingleProcess ?? GetDefaultForceSingleProcess() ? 1 : Math.Max(1, Environment.ProcessorCount * (compilationOptions?.NumberProcessPerCore ?? GetDefaultNumberProcessPerCore()));

        [XmlElement(ElementName = "NumberProcessPerCore")]
        public byte? NumberProcessPerCore { get; set; }
        public static byte GetDefaultNumberProcessPerCore() => 1;

        [XmlElement(ElementName = "MinimumNumberOfFilesPerProcess")]
        public int? MinimumNumberOfFilesPerProcess { get; set; }
        public static int GetDefaultMinimumNumberOfFilesPerProcess() => 10;
            
        [XmlElement(ElementName = "UseSimplerAnalysisForDatabaseReference")]
        public bool? UseSimplerAnalysisForDatabaseReference { get; set; }
        public static bool GetDefaultUseSimplerAnalysisForDatabaseReference() => false;
    }
}