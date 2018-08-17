﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (DatabaseReference.cs) is part of Oetools.Utilities.
// 
// Oetools.Utilities is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Utilities is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Utilities. If not, see <http://www.gnu.org/licenses/>.
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

        public static OeDatabaseReference New(DatabaseReference dbReference) {
            switch (dbReference) {
                case DatabaseReferenceSequence sequence:
                    return new OeDatabaseReferenceSequence {
                        QualifiedName = sequence.QualifiedName
                    };
                case DatabaseReferenceTable table:
                    return new OeDatabaseReferenceTable {
                        QualifiedName = table.QualifiedName,
                        Crc = table.Crc
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [XmlAttribute(AttributeName = "QualifiedName")]
        public string QualifiedName { get; set; }
    }
    
    [Serializable]
    public class OeDatabaseReferenceSequence : OeDatabaseReference {
    }
    
    [Serializable]
    public class OeDatabaseReferenceTable : OeDatabaseReference {
        
        [XmlAttribute(AttributeName = "Crc")]
        public string Crc { get; set; }
    }
}