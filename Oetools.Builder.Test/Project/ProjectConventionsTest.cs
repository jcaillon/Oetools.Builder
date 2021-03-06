#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ProjectConventionsTest.cs) is part of Oetools.Builder.Test.
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using DotUtilities.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Oetools.Builder.Test.Project {

    [TestClass]
    public class ProjectConventionsTest {

        private const string EnumMethodPrefix = "GetEnum";

        [TestMethod]
        public void CheckForGetEnumMethodsAssociatedWithProperties() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var methodInfo in type.GetMethods().ToList().Where(m => m.Name.StartsWith(EnumMethodPrefix))) {
                    var prop = type.GetProperty(methodInfo.Name.Replace(EnumMethodPrefix, ""));
                    Assert.IsNotNull(prop, $"If {methodInfo.Name} exists, we should find the property {methodInfo.Name.Replace(EnumMethodPrefix, "")} which is not the case...");
                    Assert.IsTrue(TestHelper.CanBeNull(methodInfo.ReturnType), $"Return type of {type.Name}.{methodInfo.Name} is not nullable!");
                    Assert.IsTrue(methodInfo.ReturnType.GenericTypeArguments[0].IsEnum, $"Return type of {type.Name}.{methodInfo.Name} is not an enum!");
                }
            }
        }

        [TestMethod]
        public void CheckForGetDefaultMethodsAssociatedWithProperties() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}.{nameof(Oetools.Builder.Project.Properties)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var property in type.GetProperties()) {
                    if (!property.CanRead || !property.CanWrite || property.PropertyType.IsNotPublic) {
                        continue;
                    }
                    if (property.Name.Equals("Name")) {
                        continue;
                    }
                    if (Attribute.GetCustomAttribute(property, typeof(DefaultValueMethodAttribute), true) is DefaultValueMethodAttribute attribute) {
                        if (string.IsNullOrEmpty(attribute.MethodName)) {
                            throw new Exception($"The property {type.Name}.{property.Name} which is of value type has a {nameof(DefaultValueMethodAttribute)} attribute defined to return null.");
                        }
                        var methodInfo = type.GetMethod(attribute.MethodName, BindingFlags.Public | BindingFlags.Static| BindingFlags.FlattenHierarchy);
                        Assert.IsNotNull(methodInfo, $"Can't find method {attribute.MethodName} in {type.Name}.");
                        Assert.IsTrue(property.PropertyType.IsAssignableFrom(methodInfo.ReturnType), $"The method {methodInfo.Name} should return the same type as {methodInfo.Name}");
                        Assert.AreEqual($"GetDefault{property.Name}", methodInfo.Name, "The default method should be GetDefault{property}");
                    } else {
                        if (property.PropertyType.GenericTypeArguments.Length == 1 && property.PropertyType.GenericTypeArguments[0].IsValueType) {
                            throw new Exception($"The property {type.Name}.{property.Name} which is of value type, does not have a {nameof(DefaultValueMethodAttribute)} attribute defined to get its default value.");
                        }
                    }

                }
            }
        }

        /// <summary>
        /// We need every single public property in the <see cref="Oetools.Builder.Project"/> namespace to be nullable
        /// </summary>
        [TestMethod]
        public void CheckForNullablePublicPropertiesOnly() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var propertyInfo in type.GetProperties().ToList().Where(p => p.PropertyType.IsPublic)) {
                    Assert.IsTrue(TestHelper.CanBeNull(propertyInfo.PropertyType), $"{type.Name}.{propertyInfo.Name} is not nullable!");
                }
            }
        }

        /// <summary>
        /// We need every single public property in the <see cref="Oetools.Builder.Project"/> to have an xml name defined
        /// </summary>
        [TestMethod]
        public void CheckForXmlPropertiesOnEachPublicProperties() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var propertyInfo in type.GetProperties().ToList().Where(p => p.PropertyType.IsPublic && !(p.GetSetMethod()?.IsAbstract ?? false))) {
                    Assert.IsTrue(
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAnyAttributeAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlArrayAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlElementAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlIgnoreAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlTextAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute), true) != null,
                        $"{type.Name}.{propertyInfo.Name} does not have an xml attribute!");
                }
            }
        }

        [TestMethod]
        public void CheckUniqueXmlNameOnEachPublicProperties() {
            var xmlLeafName = new HashSet<string>();
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}.{nameof(Oetools.Builder.Project.Properties)}")) {
                if (!type.IsPublic || type.IsAbstract) {
                    continue;
                }
                foreach (var propertyInfo in type.GetProperties()) {
                    if (propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType.GenericTypeArguments.Length == 1 && propertyInfo.PropertyType.GenericTypeArguments[0].IsValueType) {
                        string name = null;
                        if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlElementAttribute), true) is XmlElementAttribute attr2) {
                            name = attr2.ElementName;
                        }
                        if (Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute), true) is XmlAttributeAttribute attr3) {
                            name = attr3.AttributeName;
                        }
                        if (string.IsNullOrEmpty(name)) {
                            Console.WriteLine($"Xml name null for {type.FullName}.{propertyInfo.Name}.");
                            continue;
                        }
                        if (!string.IsNullOrEmpty(name) && name.Equals("Name")) {
                            continue;
                        }
                        Assert.IsFalse(xmlLeafName.Contains(name), $"The xml name {name} is duplicated! {type.FullName}.{propertyInfo.Name}.");
                        xmlLeafName.Add(name);
                    }
                }
            }
        }

        [TestMethod]
        public void AllSerializableClassInProjectShouldSerialize() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                var attr = type.GetCustomAttributes(typeof(SerializableAttribute), true);
                if (attr != null && attr.Length > 0 && type.IsPublic) {
                    try {
                        new XmlSerializer(type);
                    } catch (Exception e) {
                        Assert.IsNull(e, $"FAILED TO SERIALIZE : {type.Name} : {e}");
                    }
                }
            }
        }


    }
}
