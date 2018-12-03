#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (XmlDocumentWriter.cs) is part of Oetools.Builder.
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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Oetools.Builder.Utilities {
    
    internal static class XmlDocumentWriter {
        
        /// <summary>
        /// Saves an object into an xml using a serializer, also strips all the node like : &lt;Node xsi:nil="true" /&gt;
        /// </summary>
        /// <param name="path"></param>
        /// <param name="serializer"></param>
        /// <param name="obj"></param>
        internal static void Save(string path, XmlSerializer serializer, object obj) {
            var schemePrefix = "xsi";
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(schemePrefix, XmlSchema.InstanceNamespace);

            XmlDocument doc = new XmlDocument();
            XPathNavigator nav = doc.CreateNavigator();
            using (XmlWriter w = nav.AppendChild()) {
                serializer.Serialize(w, obj, namespaces);
            }

            if (doc.DocumentElement != null) {
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
                bool schemeExist = false;
                foreach (XmlAttribute attr in doc.DocumentElement.Attributes) {
                    if (attr.Prefix.Equals("xmlns", StringComparison.InvariantCultureIgnoreCase) && attr.LocalName.Equals(schemePrefix, StringComparison.InvariantCultureIgnoreCase)) {
                        nsMgr.AddNamespace(attr.LocalName, attr.Value);
                        schemeExist = true;
                        break;
                    }
                }

                if (schemeExist) {
                    XmlNodeList xmlNodeList = doc.SelectNodes("//*[@" + schemePrefix + ":nil='true']", nsMgr);
                    if (xmlNodeList != null) {
                        foreach (XmlNode xmlNode in xmlNodeList) {
                            xmlNode.ParentNode?.RemoveChild(xmlNode);
                        }
                    }
                }
                
                // remove empty nodes
                var xmlEmptyNodeList = doc.SelectNodes("//*[not(node()) and not(@*) and not(text())]");
                if (xmlEmptyNodeList != null) {
                    foreach (XmlNode xmlNode in xmlEmptyNodeList) {
                        xmlNode.ParentNode?.RemoveChild(xmlNode);
                    }
                }
            }

            doc.Save(path);
        }
    }
}