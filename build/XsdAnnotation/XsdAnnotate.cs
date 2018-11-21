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
        
        private const string DefaultMethodPrefix = "GetDefault";
        private const string EnumMethodPrefix = "GetEnum";
        
        private readonly List<Type> _existingTypes;
        private readonly List<XElement> _xmlDocElementsList;
        private XNamespace _xsNs;
        private Dictionary<string, string> _enumOptions = new Dictionary<string, string>();
        private Dictionary<string, string> _defaultValues = new Dictionary<string, string>();
        
        public XsdAnnotate(List<Type> existingTypes, List<XElement> xmlDocElementsList) {
            _existingTypes = existingTypes;
            _xmlDocElementsList = xmlDocElementsList;
        }

        public void Annotate(string xsdPath, string outputXsdPath) {
            var doc = XDocument.Load(xsdPath);
            _xsNs = ((XElement) doc.FirstNode).GetNamespaceOfPrefix("xs");

            FindEnumOptions();
            FindDefaultValues();
            
            foreach (var element in doc.Descendants(_xsNs + "element").Concat(doc.Descendants(_xsNs + "attribute")).Concat(doc.Descendants(_xsNs + "enumeration"))) {
                string documentationStr;
                var parentTypeName = GetParentTypeName(element);
                string propertyName = null;
                if (!string.IsNullOrEmpty(parentTypeName) && !parentTypeName.StartsWith("ArrayOf")) {
                    // case of a property
                    Type correspondingType = _existingTypes.FirstOrDefault(type => type.Name.Equals(parentTypeName));
                    propertyName = GetTypePropertyNameFromXmlMemberName(correspondingType, element.Attribute("name")?.Value ?? element.Attribute("value")?.Value);
                    documentationStr = GetDocumentationFromXmlComment(parentTypeName, propertyName);
                } else {
                    // case of a type
                    documentationStr = GetDocumentationFromXmlComment(element.Attribute("type")?.Value, null);
                }

                if (string.IsNullOrEmpty(documentationStr)) {
                    Console.Error.WriteLine($"Can't find a description for xml name {element.Attribute("name")}: {parentTypeName}{(string.IsNullOrEmpty(propertyName) ? "" : $".{propertyName}")}.");
                } else {
                    element.Add(new XElement(_xsNs + "annotation",
                        new XElement(_xsNs + "documentation", new XCData(documentationStr))
                    ));
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
            foreach (var element in doc.Descendants(_xsNs + "sequence")) {
                var parentTypeName = GetParentTypeName(element);
                if (!string.IsNullOrEmpty(parentTypeName) && !parentTypeName.StartsWith("ArrayOf")) {
                    element.Name = _xsNs + "all";
                }
            }

            doc.Save(outputXsdPath);
        } 

        private string GetDocumentationFromXmlComment(string parentTypeName, string propertyName, bool incSummary = true, bool incRemarks = true, bool incExamples = true) {
            var qualifiedMemberName = $"{parentTypeName ?? ""}{(!string.IsNullOrEmpty(parentTypeName) && !string.IsNullOrEmpty(propertyName) ? "." : "")}{propertyName ?? ""}";
            StringBuilder output = new StringBuilder();
            var memberNode = _xmlDocElementsList.FirstOrDefault(elem => elem.Attribute("name")?.Value.EndsWith(qualifiedMemberName) ?? false);
            if (memberNode != null) {
                var summaryNode = memberNode.Element("summary");
                if (incSummary && summaryNode != null) {
                    var paraNode = summaryNode.Element("para");
                    output.Append(CliCompactWhitespaces(new StringBuilder(paraNode?.Value ?? summaryNode.Value)));
                }
                if (!string.IsNullOrEmpty(propertyName) && _defaultValues.ContainsKey(propertyName)) {
                    output.Append("\n");
                    output.Append($"Defaults to: {_defaultValues[propertyName]}.");
                }
                if (!string.IsNullOrEmpty(propertyName) && _enumOptions.ContainsKey(propertyName)) {
                    output.Append("\n");
                    output.Append($"Options are: {_enumOptions[propertyName]}.");
                }
                var remarksNode = memberNode.Element("remarks");
                if (incRemarks && remarksNode != null) {
                    output.Append("\n");
                    output.Append("\n");
                    output.Append("Remarks:");
                    output.Append("\n");
                    output.Append(CliCompactWhitespaces(new StringBuilder(remarksNode.Value)));
                }
                var exampleNode = memberNode.Element("example");
                if (incExamples && exampleNode != null) {
                    output.Append("\n");
                    output.Append("\n");
                    output.Append("Examples:");
                    output.Append("\n");
                    output.Append(CliCompactWhitespaces(new StringBuilder(exampleNode.Value)));
                }
                var inheritdocNode = memberNode.Element("inheritdoc");
                if (inheritdocNode != null) {
                    var cref = inheritdocNode.Attribute("cref");
                    if (cref != null) {
                        output.Append(GetDocumentationFromXmlComment(cref.Value, null, memberNode.Element("summary") == null, memberNode.Element("remarks") == null, memberNode.Element("example") == null));
                    }
                }
            }
            return output.ToString();
        }        

        private string GetTypePropertyNameFromXmlMemberName(Type correspondingType, string methodName) {
            if (correspondingType != null) {
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
        
        private void FindDefaultValues() {
            foreach (var existingType in _existingTypes) {
                foreach (var methodInfo in existingType
                    .GetMethods(BindingFlags.Public | BindingFlags.Static| BindingFlags.FlattenHierarchy)
                    .ToList()
                    .Where(m => m.IsStatic && m.Name.StartsWith(DefaultMethodPrefix))) {
                    var propertyName = methodInfo.Name.Replace(DefaultMethodPrefix, "");
                    var prop = existingType.GetProperty(propertyName);
                    if (prop != null) {
                        string defaultValue = null;
                        if (Attribute.GetCustomAttribute(methodInfo, typeof(DescriptionAttribute), true) is DescriptionAttribute description) {
                            defaultValue = description.Description;
                        } else {
                            if (!prop.PropertyType.IsClass || prop.PropertyType == typeof(string)) {
                                defaultValue = methodInfo.Invoke(null, null).ToString();
                            }
                        }
                        if (!string.IsNullOrEmpty(defaultValue) && !_defaultValues.ContainsKey(propertyName)) {
                            _defaultValues.Add(propertyName, defaultValue);
                        }
                    }
                }
            }
        }

        private void FindEnumOptions() {
            foreach (var existingType in _existingTypes) {
                foreach (var methodInfo in existingType.GetMethods().ToList().Where(m => m.Name.StartsWith(EnumMethodPrefix))) {
                    var propertyName = methodInfo.Name.Replace(EnumMethodPrefix, "");
                    var prop = existingType.GetProperty(propertyName);
                    if (prop != null && methodInfo.ReturnType.GenericTypeArguments.Length > 0) {
                        var enumType = methodInfo.ReturnType.GenericTypeArguments[0];
                        if (!enumType.IsEnum) {
                            continue;
                        }
                        var optionsList = new StringBuilder();
                        var i = 0;
                        foreach (var name in Enum.GetNames(enumType)) {
                            if (i > 0) {
                                optionsList.Append(", ");
                            }
                            optionsList.Append(name);
                            i++;
                        }
                        if (!_enumOptions.ContainsKey(propertyName)) {
                            _enumOptions.Add(propertyName, optionsList.ToString());
                        }
                    }
                }
            }
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