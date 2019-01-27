#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeBuildHistory.cs) is part of Oetools.Builder.
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
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
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
            XmlDocumentWriter.Save(path, serializer, this);
        }
        
        #endregion

        [XmlElement(ElementName = "PackageInfo")]
        public List<OeWebclientPackage> WebclientPackageInfo { get; set; }

        /// <summary>
        /// Files built during this build
        /// </summary>
        [XmlArray("BuiltFiles")]
        [XmlArrayItem("File", typeof(OeFileBuilt))]
        public List<OeFileBuilt> BuiltFiles { get; set; }
        
        /// <summary>
        /// Converts certain public string property (representing path) into relative path
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="outputDirectory"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void ToRelativePath(string sourceDirectory, string outputDirectory) {
            Utils.ForEachPublicPropertyStringInObject(typeof(OeBuildHistory), this, (propInfo, value) => {
                if (!(Attribute.GetCustomAttribute(propInfo, typeof(BaseDirectoryAttribute), true) is BaseDirectoryAttribute attr)) {
                    return value;
                }
                switch (attr.Type) {
                    case BaseDirectoryType.SourceDirectory:
                        return value.ToRelativePath(sourceDirectory);
                    case BaseDirectoryType.OutputDirectory:
                        return value.ToRelativePath(outputDirectory);
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
                if (!(Attribute.GetCustomAttribute(propInfo, typeof(BaseDirectoryAttribute), true) is BaseDirectoryAttribute attr)) {
                    return value;
                }
                switch (attr.Type) {
                    case BaseDirectoryType.SourceDirectory:
                        return value.ToAbsolutePath(sourceDirectory);
                    case BaseDirectoryType.OutputDirectory:
                        return value.ToAbsolutePath(outputDirectory);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }
    }
}