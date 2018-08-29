#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeDatabaseReference.cs) is part of Oetools.Builder.
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
using Oetools.Utilities.Openedge;

namespace Oetools.Builder.History {
    
    /// <summary>
    /// Represent the tables or sequences that were referenced in a given .r code file and thus needed to compile
    /// also, if one reference changes, the file should be recompiled
    /// </summary>
    [Serializable]
    public class OeDatabaseReference {
        
        [XmlAttribute(AttributeName = "QualifiedName")]
        public string QualifiedName { get; set; }
        
        internal static OeDatabaseReference New(UoeDatabaseReference reference) {
            switch (reference) {
                case UoeDatabaseReferenceSequence _:
                    return new OeDatabaseReferenceSequence {
                        QualifiedName = reference.QualifiedName
                    };
                case UoeDatabaseReferenceTable table:
                    return new OeDatabaseReferenceTable {
                        QualifiedName = reference.QualifiedName,
                        Crc = table.Crc
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(UoeDatabaseReference), reference, "no matching type");
            }
        }
    }
}