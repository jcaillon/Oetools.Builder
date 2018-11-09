﻿#region header
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
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib.Attributes;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeFile : IOeFileToBuild {

        public OeFile() { }

        public OeFile(string sourceFilePath) {
            Path = sourceFilePath;
        }

        /// <summary>
        /// The path of the source file (saved as relative path but absolute path during execution)
        /// </summary>
        [XmlAttribute(AttributeName = "SourceFilePath")]
        [BaseDirectory(Type = BaseDirectoryType.SourceDirectory)]
        public string Path { get; set; }

        /// <inheritdoc cref="IOeFile.LastWriteTime"/>
        [XmlAttribute(AttributeName = "LastWriteTime")]
        public DateTime LastWriteTime { get; set; }

        /// <inheritdoc cref="IOeFile.Size"/>
        [XmlAttribute(AttributeName = "Size")]
        public long Size { get; set; }

        /// <inheritdoc cref="IOeFile.Hash"/>
        [XmlAttribute(AttributeName = "Md5")]
        public string Hash { get; set; }
        
        /// <inheritdoc cref="IOeFile.State"/>
        [XmlElement(ElementName = "State")]
        public OeFileState State { get; set; }

        private string _sourcePathForTaskExecution;
        
        /// <inheritdoc cref="IOeFileToBuild.SourcePathForTaskExecution"/>
        [XmlIgnore]
        [DeepCopy(Ignore = true)]
        public string SourcePathForTaskExecution {
            get => _sourcePathForTaskExecution ?? Path;
            set => _sourcePathForTaskExecution = value;
        }

        /// <inheritdoc cref="IOeFileToBuild.TargetsToBuild"/>
        [XmlIgnore]
        [DeepCopy(Ignore = true)]
        public List<AOeTarget> TargetsToBuild { get; set; }
        
        public override string ToString() => Path;
    }
}