#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeProjectDatabase.cs) is part of Oetools.Builder.
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

namespace Oetools.Builder.Project.Properties {
    
    /// <summary>
    /// An openedge database representation, consisting of a data definition file and the database logical name.
    /// </summary>
    [Serializable]
    public class OeProjectDatabase {
      
        /// <summary>
        /// The logical name of the database.
        /// </summary>
        /// <remarks>
        /// This is the database name used in the code.
        /// </remarks>
        [XmlAttribute(AttributeName = "LogicalName")]
        public string LogicalName { get; set; }
            
        /// <summary>
        /// The path to the data definition (.df) file representing the schema of your database.
        /// </summary>
        /// <remarks>
        /// From this file, the tool is able to generate a temporary database used to compile your application.
        /// </remarks>
        [XmlAttribute(AttributeName = "DataDefinitionFilePath")]
        public string DataDefinitionFilePath { get; set; }
        
    }
}