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
using System.Linq;
using System.Xml.Serialization;
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
        

        private const string DefaultMethodPrefix = "GetDefault";
        
        [TestMethod]
        public void CheckForGetDefaultMethodsAssociatedWithProperties() {
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                if (!type.IsPublic) {
                    continue;
                }
                foreach (var methodInfo in type.GetMethods().ToList().Where(m => m.IsStatic && m.Name.StartsWith(DefaultMethodPrefix))) {                   
                    var prop = type.GetProperty(methodInfo.Name.Replace(DefaultMethodPrefix, ""));
                    Assert.IsNotNull(prop, $"if {methodInfo.Name} exists, we should find the property {methodInfo.Name.Replace(DefaultMethodPrefix, "")} which is not the case...");
                    //Assert.AreEqual(methodInfo.ReturnType, prop.PropertyType, $"The method {methodInfo.Name} should return the same type as {methodInfo.Name.Replace(DefaultMethodPrefix, "")}");
                    Assert.IsTrue(prop.PropertyType.IsAssignableFrom(methodInfo.ReturnType), $"The method {methodInfo.Name} should return the same type as {methodInfo.Name.Replace(DefaultMethodPrefix, "")}");
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
        /// We need every single public property in the <see cref="Oetools.Builder.Project"/> namespace to be nullable
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
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute), true) != null, 
                        $"{type.Name}.{propertyInfo.Name} does not have an xml attribute!");
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