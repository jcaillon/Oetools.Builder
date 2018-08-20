#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeFile.cs) is part of Oetools.Builder.
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
using System.Xml.Serialization;
using Oetools.Builder.Utilities;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Attributes;

namespace Oetools.Builder.History {
    
    [Serializable]
    public class OeFile : IOeFileToBuildTargetFile, IOeFileToBuildTargetArchive {

        public OeFile() { }

        public OeFile(string sourceFilePath) {
            SourceFilePath = sourceFilePath;
        }

        /// <summary>
        /// The relative path of the source file
        /// </summary>
        [XmlAttribute(AttributeName = "SourceFilePath")]
        [BaseDirectory(Type = BaseDirectoryType.SourceDirectory)]
        public string SourceFilePath { get; set; }

        [XmlAttribute(AttributeName = "LastWriteTime")]
        public DateTime LastWriteTime { get; set; }

        [XmlAttribute(AttributeName = "Size")]
        public long Size { get; set; }

        /// <summary>
        ///     MD5
        /// </summary>
        [XmlAttribute(AttributeName = "Md5")]
        public string Hash { get; set; }
        
        /// <summary>
        /// Represents the state of the file for this build compare to the previous one
        /// </summary>
        [XmlElement(ElementName = "State")]
        public OeFileState State { get; set; }

        private string _sourcePathForTaskExecution;
        
        /// <summary>
        /// Can be different from <see cref="SourceFilePath"/> for instance in the case of a .p, <see cref="SourcePathForTaskExecution"/>
        /// will be set to the path of the .r code to copy instead of the actual source path
        /// </summary>
        [XmlIgnore]
        [DeepCopy(Ignore = true)]
        public string SourcePathForTaskExecution {
            get => _sourcePathForTaskExecution ?? SourceFilePath;
            set => _sourcePathForTaskExecution = value;
        }

        [XmlIgnore]
        [DeepCopy(Ignore = true)]
        public List<OeTargetArchive> TargetsArchives { get; set; }

        [XmlIgnore]
        [DeepCopy(Ignore = true)]
        public List<OeTargetFile> TargetsFiles { get; set; }
        
        public OeFile GetDeepCopy() {
            return (OeFile) Utils.DeepCopyPublicProperties(this, typeof(OeFile));
        }

        public override string ToString() => SourceFilePath;
    }
}