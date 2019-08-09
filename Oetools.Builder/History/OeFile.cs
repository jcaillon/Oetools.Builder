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
using System.Linq;
using System.Xml.Serialization;
using DotUtilities;
using DotUtilities.Attributes;
using DotUtilities.Extensions;
using Oetools.Builder.Utilities.Attributes;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeFile : IOeFileToBuild {

        public OeFile() { }

        public OeFile(string sourceFilePath) {
            Path = sourceFilePath;
        }

        public OeFile(IOeFile sourceFile) {
            sourceFile.DeepCopy(this);
        }

        /// <summary>
        /// The path of the source file (saved as relative path but absolute path during execution)
        /// </summary>
        [XmlAttribute(AttributeName = "SourceFilePath")]
        [BaseDirectory(Type = BaseDirectoryType.SourceDirectory)]
        public string Path { get; set; }

        /// <inheritdoc cref="IOeFile.State"/>
        [XmlIgnore]
        public OeFileState State { get; set; }

        /// <inheritdoc cref="IOeFile.State"/>
        [XmlAttribute(AttributeName = "State")]
        public string StateString {
            get => State.ToString();
            set {
                if (Enum.TryParse(value, true, out OeFileState state)) {
                    State = state;
                }
            }
        }

        /// <inheritdoc cref="IOeFile.LastWriteTime"/>
        [XmlAttribute(AttributeName = "LastWriteTime")]
        public DateTime LastWriteTime { get; set; }

        /// <inheritdoc cref="IOeFile.Size"/>
        [XmlAttribute(AttributeName = "Size")]
        public long Size { get; set; }

        /// <inheritdoc cref="IOeFile.Checksum"/>
        [XmlAttribute(AttributeName = "Checksum")]
        public string Checksum { get; set; }

        private string _sourcePathForTaskExecution;

        /// <inheritdoc cref="IOeFileToBuild.PathForTaskExecution"/>
        [XmlIgnore]
        [DeepCopy(Ignore = true)]
        public string PathForTaskExecution {
            get => _sourcePathForTaskExecution ?? Path;
            set => _sourcePathForTaskExecution = value;
        }

        /// <inheritdoc cref="IOeFileToBuild.TargetsToBuild"/>
        [XmlIgnore]
        [DeepCopy(Ignore = true)]
        public List<AOeTarget> TargetsToBuild { get; set; }

        public override string ToString() => Path;

        public static PathList<IOeFileToBuild> ConvertToFileToBuild(IEnumerable<IOeFile> files) {
            return files?.Select(f => {
                if (f is IOeFileToBuild ftb) {
                    return ftb;
                }
                return new OeFile(f);
            }).ToFileList();
        }
    }
}
