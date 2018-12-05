#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (Program.cs) is part of ConsoleApplication1.
// 
// ConsoleApplication1 is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ConsoleApplication1 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ConsoleApplication1. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XsdAnnotator {
    /// <summary>
    /// This class needs to be refactored and generalized... quick and dirty work indeed...
    /// </summary>
    internal class XsdAnnotate {
        private const string EnumMethodPrefix = "GetEnum";
        private const string DefaultMethodPrefix = "GetDefault";

        private readonly List<Type> _existingTypes;
        private readonly List<XElement> _xmlDocElementsList;
        private XNamespace _xsNs;
        private Dictionary<string, Documentation> _xmlNameDocumentation = new Dictionary<string, Documentation>();
        
        public XsdAnnotate(List<Type> existingTypes, List<XElement> xmlDocElementsList) {
            _existingTypes = existingTypes;
            _xmlDocElementsList = xmlDocElementsList;
        }

        public void Annotate(string xsdPath, string outputXsdPath) {
            var xsdDocument = XDocument.Load(xsdPath);
            _xsNs = ((XElement) xsdDocument.FirstNode).GetNamespaceOfPrefix("xs");

            GetXmlDocumentation();

            foreach (var element in xsdDocument.Descendants(_xsNs + "element").Concat(xsdDocument.Descendants(_xsNs + "attribute")).Concat(xsdDocument.Descendants(_xsNs + "enumeration"))) {
                Documentation documentation = null;
                
                var parentTypeName = GetParentTypeName(element);
                
                // case of a property
                var xmlNameQualifiedWithClassName = $"{parentTypeName}.{element.Attribute("name")?.Value ?? ""}";
                if (_xmlNameDocumentation.ContainsKey(xmlNameQualifiedWithClassName)) {
                    documentation = _xmlNameDocumentation[xmlNameQualifiedWithClassName];
                } else {
                    // case of a type
                    var typeName = element.Attribute("type")?.Value;
                    if (!string.IsNullOrEmpty(typeName) && _xmlNameDocumentation.ContainsKey(typeName)) {
                        documentation = _xmlNameDocumentation[typeName];
                    }
                }

                if (documentation == null) {
                    Console.Error.WriteLine($"Can't find a description for xml name: {element.Attribute("name")?.Value ??  element.Attribute("type")?.Value ?? "unknown"}.");
                } else {
                    var sb = new StringBuilder();
                    sb.Append(documentation.Summary);
                    if (!string.IsNullOrEmpty(documentation.DefaultValue)) {
                        sb.Append("\n").Append("\n").Append("<b>Defaults to:</b> ").Append(documentation.DefaultValue).Append(".");
                    }
                    if (!string.IsNullOrEmpty(documentation.AcceptableValues)) {
                        sb.Append("\n").Append("\n").Append("<b>Available options are:</b> ").Append(documentation.AcceptableValues).Append(".");
                    }
                    if (!string.IsNullOrEmpty(documentation.Remarks)) {
                        sb.Append("\n").Append("\n").Append("<b>Remarks:</b>").Append("\n").Append(documentation.Remarks);
                    }
                    if (!string.IsNullOrEmpty(documentation.Examples)) {
                        sb.Append("\n").Append("\n").Append("<b>Examples:</b>").Append("\n").Append(documentation.Examples);
                    }
                    sb.Replace("\n", "<br>\n");
                    element.Add(new XElement(_xsNs + "annotation", new XElement(_xsNs + "documentation", new XCData(sb.ToString()))));
                }

                // Correct the minOccurs for nullable elements!
                var nillableAttr = element.Attribute("nillable");
                if (nillableAttr != null && nillableAttr.Value.Equals("true")) {
                    var minOccursAttr = element.Attribute("minOccurs");
                    if (minOccursAttr != null) {
                        minOccursAttr.Value = "0";
                    }
                }
            }

            // replace xs:sequence by xs:all to be able to input elements in any order!
            foreach (var element in xsdDocument.Descendants(_xsNs + "sequence")) {
                var parentTypeName = GetParentTypeName(element);
                if (!string.IsNullOrEmpty(parentTypeName) && !parentTypeName.StartsWith("ArrayOf")) {
                    element.Name = _xsNs + "all";
                }
            }

            xsdDocument.Save(outputXsdPath);
        }

        private static string GetParentTypeName(XElement element) {
            string parentTypeName = null;
            var parent = element.Parent;
            while (parent != null) {
                var attrName = parent.Attribute("name");
                if (attrName != null) {
                    parentTypeName = attrName.Value;
                    break;
                }

                parent = parent.Parent;
            }
            return parentTypeName;
        }

        private void GetXmlDocumentation() {
            foreach (var existingType in _existingTypes) {
                
                var typeDoc = GetDocumentation(existingType.FullName);
                if (typeDoc != null && !_xmlNameDocumentation.ContainsKey(existingType.Name)) {
                    _xmlNameDocumentation.Add(existingType.Name, typeDoc);
                }
                
                foreach (var propertyInfo in existingType.GetProperties()) {
                    string xmlName;
                    if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlArrayAttribute), true) is XmlArrayAttribute attr1) {
                        xmlName = attr1.ElementName;
                    } else if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlElementAttribute), true) is XmlElementAttribute attr2) {
                        xmlName = attr2.ElementName;
                    } else if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute), true) is XmlAttributeAttribute attr3) {
                        xmlName = attr3.AttributeName;
                    } else {
                        continue;
                    }

                    var xsdName = $"{existingType.Name}.{xmlName}";

                    if (_xmlNameDocumentation.ContainsKey(xsdName)) {
                        continue;
                    }

                    var doc = GetDocumentation($"{existingType.FullName}.{propertyInfo.Name}");
                    if (doc == null) {
                        continue;
                    }
                    
                    // default
                    var methodInfo = existingType.GetMethod($"{DefaultMethodPrefix}{propertyInfo.Name}", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (methodInfo != null) {
                        if (Attribute.GetCustomAttribute(methodInfo, typeof(DescriptionAttribute), true) is DescriptionAttribute description) {
                            doc.DefaultValue = description.Description;
                        } else if (!propertyInfo.PropertyType.IsClass || propertyInfo.PropertyType == typeof(string)) {
                            doc.DefaultValue = methodInfo.Invoke(null, null).ToString();
                        }
                    }

                    // enum
                    methodInfo = existingType.GetMethod($"{EnumMethodPrefix}{propertyInfo.Name}", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (methodInfo != null && methodInfo.ReturnType.GenericTypeArguments.Length > 0) {
                        var enumType = methodInfo.ReturnType.GenericTypeArguments[0];
                        if (!enumType.IsEnum) {
                            continue;
                        }
                        var optionsList = new StringBuilder();
                        var i = 0;
                        foreach (var name in Enum.GetNames(enumType)) {
                            if (i > 0) {
                                optionsList.Append(" or ");
                            }
                            optionsList.Append(name);
                            i++;
                        }
                        doc.AcceptableValues = optionsList.ToString();
                    }
                    
                    _xmlNameDocumentation.Add(xsdName, doc);
                }
            }
        }

        private class Documentation {
            public string Summary { get; set; }
            public string Remarks { get; set; }
            public string Examples { get; set; }
            public string DefaultValue { get; set; }
            public string AcceptableValues { get; set; }
        }

        private Documentation GetDocumentation(string memberName, Documentation doc = null) {
            var memberNode = _xmlDocElementsList.FirstOrDefault(elem => elem.Attribute("name")?.Value.EndsWith(memberName) ?? false);
            if (memberNode == null) {
                return null;
            }

            if (doc == null) {
                doc = new Documentation();
            }

            if (string.IsNullOrEmpty(doc.Summary)) {
                var summaryNode = memberNode.Element("summary");
                if (summaryNode != null) {
                    var paraNode = summaryNode.Element("para");
                    //doc.Summary = CliCompactWhitespaces(new StringBuilder(paraNode?.Value ?? summaryNode.Value)).ToString();
                    doc.Summary = StripIndentation(paraNode?.Value ?? summaryNode.Value);
                }
            }

            if (string.IsNullOrEmpty(doc.Remarks)) {
                var remarksNode = memberNode.Element("remarks");
                if (remarksNode != null) {
                    var paraNode = remarksNode.Element("para");
                    //doc.Remarks = CliCompactWhitespaces(new StringBuilder(paraNode?.Value ?? remarksNode.Value)).ToString();
                    doc.Remarks = StripIndentation(paraNode?.Value ?? remarksNode.Value);
                }
            }

            if (string.IsNullOrEmpty(doc.Examples)) {
                var exampleNode = memberNode.Element("example");
                if (exampleNode != null) {
                    var paraNode = exampleNode.Element("para");
                    //doc.Examples = CliCompactWhitespaces(new StringBuilder(paraNode?.Value ?? exampleNode.Value)).ToString();
                    doc.Examples = StripIndentation(paraNode?.Value ?? exampleNode.Value);
                }
            }

            var inheritdocNode = memberNode.Element("inheritdoc");
            var cref = inheritdocNode?.Attribute("cref");
            if (cref != null) {
                doc = GetDocumentation(cref.Value, doc);
            }

            return doc;
        }

        private string StripIndentation(string p0) {
            var idx = p0.LastIndexOf('\n');
            if (idx > -1) {
                var nbSpace = p0.Length - idx;
                if (nbSpace > 0) {
                    return p0.Replace("\n".PadRight(nbSpace), "\n").Trim('\n');
                }
            }
            return p0;
        }

        /// <summary>
        /// handle all whitespace chars not only spaces, trim both leading and trailing whitespaces, remove extra internal whitespaces,
        /// and all whitespaces are replaced to space char (so we have uniform space separator)
        /// Will not compact whitespaces inside quotes or double quotes
        /// </summary>
        /// <param name="sb"></param>
        private static StringBuilder CliCompactWhitespaces(StringBuilder sb) {
            if (sb == null)
                return null;
            if (sb.Length == 0)
                return sb;

            // set [start] to first not-whitespace char or to sb.Length
            int start = 0;
            while (start < sb.Length) {
                if (char.IsWhiteSpace(sb[start]))
                    start++;
                else
                    break;
            }

            // if [sb] has only whitespaces, then return empty string
            if (start == sb.Length) {
                sb.Length = 0;
                return sb;
            }

            // set [end] to last not-whitespace char
            int end = sb.Length - 1;
            while (end >= 0) {
                if (char.IsWhiteSpace(sb[end]))
                    end--;
                else
                    break;
            }

            // compact string
            int dest = 0;
            bool previousIsWhitespace = false;
            for (int i = start; i <= end; i++) {
                if (char.IsWhiteSpace(sb[i]) && sb[i] != '\n') {
                    if (!previousIsWhitespace) {
                        previousIsWhitespace = true;
                        sb[dest] = ' ';
                        dest++;
                    }
                } else {
                    previousIsWhitespace = sb[i] == '\n';
                    sb[dest] = sb[i];
                    dest++;
                }
            }

            sb.Length = dest;
            return sb;
        }
    }
}