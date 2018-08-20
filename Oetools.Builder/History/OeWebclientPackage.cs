#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeWebclientPackage.cs) is part of Oetools.Builder.
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

namespace Oetools.Builder.History {
    [Serializable]
    public class OeWebclientPackage {

        [XmlElement(ElementName = "VendorName")]
        public string VendorName { get; set; }

        [XmlElement(ElementName = "ApplicationName")]
        public string ApplicationName { get; set; }
        
        /// <summary>
        /// Prowcapp version, automatically computed by this tool
        /// </summary>
        [XmlElement(ElementName = "WebclientProwcappVersion")]
        public int WebclientProwcappVersion { get; set; }
    }
}