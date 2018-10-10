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
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace XsdAnnotator {
    
    internal class XsdAnnotate {
        
        private readonly List<Type> _existingTypes;
        
        public XsdAnnotate(List<Type> existingTypes) {
            _existingTypes = existingTypes;
        }

        public void Annotate(string xsdPath, string outputXsdPath) {
            XmlSchema schema;
            using (var reader = new StreamReader(xsdPath, false)) {
                schema = XmlSchema.Read(reader, (sender, args2) => {
                    Console.Error.WriteLine(args2.Message);
                });
                foreach (var complexType in schema.Items.OfType<XmlSchemaComplexType>()) {
                    HandleComplexType(complexType);
                }
            }
            using (var writer = new StreamWriter(outputXsdPath, false)) {
                schema.Write(writer);
            }
        }
        
        private void HandleComplexType(XmlSchemaComplexType complexType) {

            if (complexType.Name.StartsWith("ArrayOf")) {
                if (complexType.Particle is XmlSchemaSequence seq) {
                    foreach (var element in seq.Items.OfType<XmlSchemaElement>()) {
                        if (GetTypeFromTypeName(element.SchemaTypeName.Name)?.GetCustomAttributes(
                            typeof(DescriptionAttribute), true
                        ).FirstOrDefault() is DescriptionAttribute description) {
                            element.Annotation = GetAnnotation(description.Description);
                        } else {
                            if (!element.SchemaTypeName.Name.Equals("string")) {
                                Console.Error.WriteLine($"Can't find a description for the class : {element.SchemaTypeName.Name}. Use the [Description] attribute! ({complexType.Name} / {element.Name})");
                                element.Annotation = GetAnnotation($"Unavailable description for the class : {element.SchemaTypeName.Name}.");
                            }
                        }
                    }
                } else if (complexType.Particle is XmlSchemaChoice choice) {
                    foreach (var element in choice.Items.OfType<XmlSchemaElement>()) {
                        if (GetTypeFromTypeName(element.SchemaTypeName.Name)?.GetCustomAttributes(
                            typeof(DescriptionAttribute), true
                        ).FirstOrDefault() is DescriptionAttribute description) {
                            element.Annotation = GetAnnotation(description.Description);
                        } else {
                            if (!element.SchemaTypeName.Name.Equals("string")) {
                                Console.Error.WriteLine($"Can't find a description for the class : {element.SchemaTypeName.Name}. Use the [Description] attribute! ({complexType.Name} / {element.Name})");
                                element.Annotation = GetAnnotation($"Unavailable description for the class : {element.SchemaTypeName.Name}.");
                            }
                        }
                    }
                }
                return;
            }
            
            Type correspondingType = GetTypeFromTypeName(complexType.Name);
            if (correspondingType == null) {
                return;
            }

            foreach (var attribute in complexType.Attributes.OfType<XmlSchemaAttribute>()) {
                attribute.Annotation = GetAnnotation(GetDescriptionFromProperty(attribute.Name, correspondingType));
            }
            switch (complexType.Particle) {
                case XmlSchemaSequence sequence:
                    foreach (var element in sequence.Items.OfType<XmlSchemaElement>()) {
                        element.Annotation = GetAnnotation(GetDescriptionFromProperty(element.Name, correspondingType));
                    }
                    break;
                case XmlSchemaChoice choice:
                    foreach (var element in choice.Items.OfType<XmlSchemaElement>()) {
                        element.Annotation = GetAnnotation(GetDescriptionFromProperty(element.Name, correspondingType));
                    }
                    break;
                default:
                    if (complexType.ContentModel is XmlSchemaComplexContent complex && complex.Content is XmlSchemaComplexContentExtension extension) {
                        foreach (var attribute in extension.Attributes.OfType<XmlSchemaAttribute>()) {
                            attribute.Annotation = GetAnnotation(GetDescriptionFromProperty(attribute.Name, correspondingType));
                        }
                        switch (extension.Particle) {
                            case XmlSchemaSequence sequence:
                                foreach (var element in sequence.Items.OfType<XmlSchemaElement>()) {
                                    element.Annotation = GetAnnotation(GetDescriptionFromProperty(element.Name, correspondingType));
                                }
                                break;
                            case XmlSchemaChoice choice:
                                foreach (var element in choice.Items.OfType<XmlSchemaElement>()) {
                                    element.Annotation = GetAnnotation(GetDescriptionFromProperty(element.Name, correspondingType));
                                }
                                break;
                            default:
                                return;
                        }
                    }
                    return;
            }
        }

        private Type GetTypeFromTypeName(string name) {
            return _existingTypes.FirstOrDefault(type => type.Name.Equals(name));
        }
        
        private XmlSchemaAnnotation GetAnnotation(string propertyName) {      
            var output = new XmlSchemaAnnotation();
            var documentation = new XmlSchemaDocumentation();
            output.Items.Add(documentation);
            documentation.Markup = TextToNodeArray(propertyName);
            return output;
        }
        
        private string GetDescriptionFromProperty(string propertyName, Type correspondingType) {
            string descriptionFromProperty = null;
            
            // get the description
            foreach (var propertyInfo in correspondingType.GetProperties().ToList().Where(p => p.PropertyType.IsPublic)) {
                if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlArrayAttribute), true) is XmlArrayAttribute attr) {
                    if (!attr.ElementName.Equals(propertyName)) {
                        continue;
                    }
                } else if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlElementAttribute), true) is XmlElementAttribute attr2) {
                    if (!attr2.ElementName.Equals(propertyName)) {
                        continue;
                    }
                } else if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute), true) is XmlAttributeAttribute attr3) {
                    if (!attr3.AttributeName.Equals(propertyName)) {
                        continue;
                    }
                }
                if (Attribute.GetCustomAttribute(propertyInfo, typeof(DescriptionAttribute), true) is DescriptionAttribute description) {
                    descriptionFromProperty = description.Description;
                }
            }
            if (string.IsNullOrEmpty(descriptionFromProperty)) {
                Console.Error.WriteLine($"Can't find a description for the property : {correspondingType.Name}.{propertyName}. Use the [Description] attribute!");
            }
            return descriptionFromProperty ?? $"Unavailable description for the property : {correspondingType.Name}.{propertyName}.";
        }
        
        private XmlNode[] TextToNodeArray(string text) {
            XmlDocument doc = new XmlDocument();
            return new XmlNode[] { doc.CreateTextNode(text) };
        }
        
    }
}