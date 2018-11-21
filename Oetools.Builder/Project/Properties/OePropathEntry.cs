#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OePropathEntry.cs) is part of Oetools.Builder.
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

using System.Xml.Serialization;

namespace Oetools.Builder.Project.Properties {
    
    /// <summary>
    /// An entry to add to the propath.
    /// </summary>
    public class OePropathEntry {
        
        /// <summary>
        /// The path of this propath entry.
        /// </summary>
        /// <remarks>
        /// This typically is a pro library (.pl) file path or a directory path.
        /// Relative path are resolved with the current directory but you can use {{SOURCE_DIRECTORY}} to target the source directory.
        /// You can use semi-colons (i.e. ;) to separate several path values.
        /// </remarks>
        [XmlText]
        public string Path { get; set; }
    }
}