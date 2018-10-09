#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (XsdAnnotationTest.cs) is part of Oetools.Builder.Test.
// 
// Oetools.Builder.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Oetools.Builder.Test {
    
    /// <summary>
    /// This class is not actually a test, it allows to annotate the automatically generated .xsd file
    /// with description taken from the properties or classes
    ///
    /// Annotate a serialize field with the [Description] attribute
    /// If a field is serialized as an array/list of objects, annotate the object class instead
    /// </summary>
    [TestClass]
    public class XsdAnnotationTest {

        [TestMethod]
        public void AnnotateXsd() {
            var path = @"C:\Users\jcaillon\Desktop\try\.oe\Project.xsd";
            if (!File.Exists(path)) {
                return;
            }

            using (var reader = new StreamReader(path, false)) {
                var schema = XmlSchema.Read(reader, (sender, args) => {
                    Console.WriteLine(args.Message);
                });
                foreach (var complexType in schema.Items.OfType<XmlSchemaComplexType>()) {
                    HandleComplexType(complexType);
                }
                using (var writer = new StreamWriter($"{path}2.xsd", false)) {
                    schema.Write(writer);
                }
            }
        }

        private void HandleComplexType(XmlSchemaComplexType complexType) {

            if (complexType.Name.StartsWith("ArrayOf")) {
                if (complexType.Particle is XmlSchemaSequence seq) {
                    foreach (var element in seq.Items.OfType<XmlSchemaElement>()) {
                        var description =  GetTypeFromTypeName(element.SchemaTypeName.Name)?.GetCustomAttributes(
                            typeof(System.ComponentModel.DescriptionAttribute), true
                        ).FirstOrDefault() as System.ComponentModel.DescriptionAttribute;
                        if (description != null) {
                            element.Annotation = GetAnnotation(description.Description);
                        } else {
                           //Assert.Fail($"Can't find a description for the class : {element.SchemaType.Name}. Use the [Description] attribute!");
                            element.Annotation = GetAnnotation($"Unavailable description for {element.Name}");
                        }
                    }
                } else if (complexType.Particle is XmlSchemaChoice choice) {
                    foreach (var element in choice.Items.OfType<XmlSchemaElement>()) {
                        var description =  GetTypeFromTypeName(element.SchemaTypeName.Name)?.GetCustomAttributes(
                            typeof(System.ComponentModel.DescriptionAttribute), true
                        ).FirstOrDefault() as System.ComponentModel.DescriptionAttribute;
                        if (description != null) {
                            element.Annotation = GetAnnotation(description.Description);
                        } else {
                            //Assert.Fail($"Can't find a description for the class : {element.SchemaType.Name}. Use the [Description] attribute!");
                            element.Annotation = GetAnnotation($"Unavailable description for {element.Name}");
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
            return TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")
                .FirstOrDefault(type => type.IsPublic && type.Name.Equals(name));
        }
        
        private XmlSchemaAnnotation GetAnnotation(string propertyName) {      
            var output = new XmlSchemaAnnotation();
            var documentation = new XmlSchemaDocumentation();
            output.Items.Add(documentation);
            documentation.Markup = TextToNodeArray(propertyName);
            return output;
        }
        
        private static XmlNode[] TextToNodeArray(string text) {
            XmlDocument doc = new XmlDocument();
            return new XmlNode[] { doc.CreateTextNode(text) };
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
                if (Attribute.GetCustomAttribute(propertyInfo, typeof(System.ComponentModel.DescriptionAttribute), true) is System.ComponentModel.DescriptionAttribute description) {
                    descriptionFromProperty = description.Description;
                }
            }
            if (string.IsNullOrEmpty(descriptionFromProperty)) {
                //Assert.Fail($"Can't find a description for the field : {correspondingType.Name}.{propertyName}. Use the [Description] attribute!");
            }
            return descriptionFromProperty ?? $"Unavailable description for {propertyName}";
        }
    }
}