#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeHistory.cs) is part of Oetools.Sakoe.
// 
// Oetools.Sakoe is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Sakoe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Sakoe. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.History {
    
    [Serializable]
    [XmlRoot("BuildHistory")]
    public class OeBuildHistory {     
        
        #region static

        public static OeBuildHistory Load(string path, string sourceDirectory, string outputDirectory) {
            OeBuildHistory xml;
            var serializer = new XmlSerializer(typeof(OeBuildHistory));
            using (var reader = new StreamReader(path)) {
                xml = (OeBuildHistory) serializer.Deserialize(reader);
            }
            xml.ToAbsolutePath(sourceDirectory, outputDirectory);
            return xml;
        }

        public void Save(string path, string sourceDirectory, string outputDirectory) {
            ToRelativePath(sourceDirectory, outputDirectory);
            var serializer = new XmlSerializer(typeof(OeBuildHistory));
            using (TextWriter writer = new StreamWriter(path, false)) {
                serializer.Serialize(writer, this);
            }
        }
        
        #endregion

        [XmlElement(ElementName = "PackageInfo")]
        public List<OeWebclientPackage> WebclientPackageInfo { get; set; }

        /// <summary>
        /// List of all the files deployed from the source directory
        /// </summary>
        [XmlArray("BuiltFiles")]
        [XmlArrayItem("BuiltFile", typeof(OeFileBuilt))]
        [XmlArrayItem("BuiltFileCompiled", typeof(OeFileBuiltCompiled))]
        public List<OeFileBuilt> BuiltFiles { get; set; }
        
        [XmlArray("CompilationProblems")]
        [XmlArrayItem("Error", typeof(OeOeCompilationError))]
        [XmlArrayItem("Warning", typeof(OeCompilationWarning))]
        public List<OeCompilationProblem> CompilationProblems { get; set; }
        
        /// <summary>
        /// Converts certain public string property (representing path) into relative path
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="outputDirectory"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void ToRelativePath(string sourceDirectory, string outputDirectory) {
            Utils.ForEachPublicPropertyStringInObject(typeof(OeBuildHistory), this, (propInfo, value) => {
                if (!(Attribute.GetCustomAttribute(propInfo, typeof(BaseDirectory), true) is BaseDirectory attr)) {
                    return value;
                }
                switch (attr.Type) {
                    case BaseDirectoryType.SourceDirectory:
                        return value.FromAbsolutePathToRelativePath(sourceDirectory);
                    case BaseDirectoryType.OutputDirectory:
                        return value.FromAbsolutePathToRelativePath(outputDirectory);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }
        
        /// <summary>
        /// Converts certain public string property (representing path) into absolute path
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="outputDirectory"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void ToAbsolutePath(string sourceDirectory, string outputDirectory) {
            Utils.ForEachPublicPropertyStringInObject(typeof(OeBuildHistory), this, (propInfo, value) => {
                if (!(Attribute.GetCustomAttribute(propInfo, typeof(BaseDirectory), true) is BaseDirectory attr)) {
                    return value;
                }
                switch (attr.Type) {
                    case BaseDirectoryType.SourceDirectory:
                        return Path.Combine(sourceDirectory, value.ToCleanPath());
                    case BaseDirectoryType.OutputDirectory:
                        return Path.Combine(outputDirectory, value.ToCleanPath());
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }
    }
}