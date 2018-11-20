#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeDatabaseAlias.cs) is part of Oetools.Builder.
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
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Properties {
    
    /// <summary>
    /// A database logical name / alias couple.
    /// </summary>
    [Serializable]
    public class OeDatabaseAlias : IUoeExecutionDatabaseAlias {
                
        /// <summary>
        /// The database logical name for which to create the alias.
        /// </summary>
        [XmlAttribute(AttributeName = "DatabaseLogicalName")]
        public string DatabaseLogicalName { get; set; }
        
        /// <summary>
        /// The alias name to give to the database.
        /// </summary>
        [XmlAttribute(AttributeName = "AliasLogicalName")]
        public string AliasLogicalName { get; set; }
    }
}