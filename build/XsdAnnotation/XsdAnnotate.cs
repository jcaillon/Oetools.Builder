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
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XsdAnnotator {
    
    /// <summary>
    /// This class needs to be refactored and generalized... quick and dirty work indeed...
    /// </summary>
    internal class XsdAnnotate {
        
        private const string DefaultMethodPrefix = "GetDefault";
        private readonly List<Type> _existingTypes;
        private readonly List<XElement> _xmlDocElementsList;
        private XNamespace _xsNs;
        
        public XsdAnnotate(List<Type> existingTypes, List<XElement> xmlDocElementsList) {
            _existingTypes = existingTypes;
            _xmlDocElementsList = xmlDocElementsList;
        }

        public void Annotate(string xsdPath, string outputXsdPath) {
            var doc = XDocument.Load(xsdPath);
            _xsNs = ((XElement) doc.FirstNode).GetNamespaceOfPrefix("xs");

            foreach (var element in doc.Descendants(_xsNs + "element").Concat(doc.Descendants(_xsNs + "attribute")).Concat(doc.Descendants(_xsNs + "enumeration"))) {
                string documentationStr;

                var parentTypeName = GetParentTypeName(element);
                if (!string.IsNullOrEmpty(parentTypeName) && !parentTypeName.StartsWith("ArrayOf")) {
                    // case of a property
                    Type correspondingType = GetTypeFromTypeName(parentTypeName);
                    var propertyName = GetTypePropertyNameFromXmlMemberName(correspondingType, element.Attribute("name")?.Value ?? element.Attribute("value")?.Value);
                    documentationStr = GetDocumentationFromXmlComment($"{parentTypeName}.{propertyName}");
                    var defaultValue = GetDocumentationFromXmlCommentAndDefaultValue(propertyName, correspondingType);
                    if (!string.IsNullOrEmpty(defaultValue)) {
                        documentationStr = $"{documentationStr} Defaults to \"{defaultValue}\".";
                    }
                } else {
                    // case of a type
                    documentationStr = GetDocumentationFromXmlComment(element.Attribute("type")?.Value);
                }
                
                element.Add(new XElement(_xsNs + "annotation",
                    new XElement(_xsNs + "documentation", new XCData(documentationStr))
                ));
            }

            doc.Save(outputXsdPath);
        }

        private string GetTypePropertyNameFromXmlMemberName(Type correspondingType, string methodName) {
            if (correspondingType != null) {
                // get the description
                foreach (var propertyInfo in correspondingType.GetMembers()) {
                    if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlArrayAttribute), true) is XmlArrayAttribute attr && attr.ElementName.Equals(methodName)) {
                        methodName = propertyInfo.Name;
                        break;
                    }

                    if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlElementAttribute), true) is XmlElementAttribute attr2 && attr2.ElementName.Equals(methodName)) {
                        methodName = propertyInfo.Name;
                        break;
                    }

                    if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute), true) is XmlAttributeAttribute attr3 && attr3.AttributeName.Equals(methodName)) {
                        methodName = propertyInfo.Name;
                        break;
                    }

                    if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlEnumAttribute), true) is XmlEnumAttribute attr4 && attr4.Name.Equals(methodName)) {
                        methodName = propertyInfo.Name;
                        break;
                    }
                }
            }
            return methodName;
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

        private string GetDocumentationFromXmlComment(string qualifiedMemberName) {
            var summary = _xmlDocElementsList.FirstOrDefault(elem => elem.Attribute("name")?.Value.EndsWith(qualifiedMemberName) ?? false)?.Element("summary")?.Value;
            if (!string.IsNullOrEmpty(summary)) {
                summary = CliCompactWhitespaces(new StringBuilder(summary)).ToString();
            }

            if (string.IsNullOrEmpty(summary)) {
                Console.Error.WriteLine($"Can't find a description for : {qualifiedMemberName}.");
            }
            return summary ?? $"Can't find a description for : {qualifiedMemberName}.";
        } 
        
        private string GetDocumentationFromXmlCommentAndDefaultValue(string propertyName, Type correspondingType) {
            if (correspondingType != null) {
                // get default value
                var defaultMethod = correspondingType.GetMethods().FirstOrDefault(m => m.IsStatic && m.Name.Equals($"{DefaultMethodPrefix}{propertyName}"));
                if (defaultMethod != null) {
                    string defaultValue;
                    if (Attribute.GetCustomAttribute(defaultMethod, typeof(DescriptionAttribute), true) is DescriptionAttribute description) {
                        defaultValue = description.Description;
                    } else {
                        defaultValue = defaultMethod.Invoke(null, null).ToString();
                    }
                    return defaultValue;
                }
            }
            return null;
        }
        
        private Type GetTypeFromTypeName(string name) {
            return _existingTypes.FirstOrDefault(type => type.Name.Equals(name));
        }
        
        
        /// <summary>
        /// handle all whitespace chars not only spaces, trim both leading and trailing whitespaces, remove extra internal whitespaces,
        /// and all whitespaces are replaced to space char (so we have uniform space separator)
        /// Will not compact whitespaces inside quotes or double quotes
        /// </summary>
        /// <param name="sb"></param>
        public static StringBuilder CliCompactWhitespaces(StringBuilder sb) {
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
                    previousIsWhitespace = false;
                    sb[dest] = sb[i];
                    dest++;
                }
            }
            
            sb.Length = dest;
            return sb;
        }
    }
}