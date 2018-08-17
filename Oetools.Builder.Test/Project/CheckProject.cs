#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (CheckForNullablePublicPropertiesOnly.cs) is part of Oetools.Builder.Test.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.Project;

namespace Oetools.Builder.Test.Project {
    
    [TestClass]
    public class CheckProject {
        
        /// <summary>
        /// We need every single public property in the <see cref="Oetools.Builder.Project"/> namespace to be nullable
        /// </summary>
        [TestMethod]
        public void CheckForNullablePublicPropertiesOnly() {
            var project = new OeProjectTest();
            Assert.IsNotNull(project);
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
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
            var project = new OeProjectTest();
            Assert.IsNotNull(project);
            foreach (var type in TestHelper.GetTypesInNamespace(nameof(Oetools), $"{nameof(Oetools)}.{nameof(Oetools.Builder)}.{nameof(Oetools.Builder.Project)}")) {
                foreach (var propertyInfo in type.GetProperties().ToList().Where(p => p.PropertyType.IsPublic)) {
                    Assert.IsTrue(
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAnyAttributeAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlArrayAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlElementAttribute), true) != null ||
                        Attribute.GetCustomAttribute(propertyInfo, typeof(XmlAttributeAttribute), true) != null, 
                        $"{type.Name}.{propertyInfo.Name} does not have an xml attribute!");
                }
            }
        }

    }
}