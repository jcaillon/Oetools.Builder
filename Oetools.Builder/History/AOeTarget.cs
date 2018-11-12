#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTarget.cs) is part of Oetools.Builder.
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

using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Utilities.Attributes;

namespace Oetools.Builder.History {
    
    public abstract class AOeTarget {
        
        /// <summary>
        /// if true, it means this target was actually a deletion.
        /// </summary>
        [XmlAttribute("DeletionMode")]
        public string DeletionMode { get; set; }

        /// <summary>
        /// if true, it means this target was actually a deletion.
        /// </summary>
        public bool IsDeletionMode() => !string.IsNullOrEmpty(DeletionMode);
        
        public void SetDeletionMode(bool deletionMode) => DeletionMode = deletionMode ? "1" : null;
        
        /// <summary>
        /// Path to the archive file.
        /// </summary>
        [XmlAttribute("ArchiveFilePath")]
        [BaseDirectory(Type = BaseDirectoryType.OutputDirectory)]
        public virtual string ArchiveFilePath { get; set; }
        
        /// <summary>
        /// The file path inside the archive (relative).
        /// </summary>
        [XmlAttribute("FilePath")]
        public virtual string FilePath { get; set; }
        
        public virtual string GetTargetPath() => Path.Combine(ArchiveFilePath, FilePath);

    }
}