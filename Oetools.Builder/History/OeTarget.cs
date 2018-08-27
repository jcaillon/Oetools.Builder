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

using System.Xml.Serialization;

namespace Oetools.Builder.History {
    
    public abstract class OeTarget {
        
        /// <summary>
        /// if true, it means this target was actually a deletion; otherwise it was a creation
        /// </summary>
        [XmlAttribute("DeletionMode")]
        public string DeletionMode { get; set; }

        public bool IsDeletionMode() => !string.IsNullOrEmpty(DeletionMode);
        
        public void SetDeletionMode() => DeletionMode = "1";

        public virtual string GetTargetPath() => null;

    }
}