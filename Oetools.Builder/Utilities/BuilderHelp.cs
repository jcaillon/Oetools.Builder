#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (BuilderHelp.cs) is part of Oetools.Builder.
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Oetools.Builder.Project;
using Oetools.Builder.Resources;

namespace Oetools.Builder.Utilities {
    
    public static class BuilderHelp {

        private static XDocument _xsd;
        private static XNamespace _xsNs;

        public static string GetPropertyDocumentation(string property) {
            if (_xsd == null) {
                _xsd = XsdResources.GetXsdDocument(OeProject.XsdName);
                _xsNs = ((XElement) _xsd.FirstNode).GetNamespaceOfPrefix("xs");
            }
            
            var node = _xsd.Descendants(_xsNs + "element").Concat(_xsd.Descendants(_xsNs + "attribute"))
                .FirstOrDefault(e => e.Attribute("name")?.Value.Equals(property) ?? false);

            var docNode = node?.Descendants(_xsNs + "annotation").FirstOrDefault()?.Descendants(_xsNs + "documentation").FirstOrDefault();
            if (docNode != null && docNode.FirstNode is XCData data) {
                return data.Value.Replace("<b>", "").Replace("</b>", "").Replace("<br>", "");
            }

            return null;
        }
    }
}