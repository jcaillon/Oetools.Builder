#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeProject.cs) is part of Oetools.Sakoe.
// 
// Oetools.Sakoe is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Sakoe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Sakoe. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Resources;

namespace Oetools.Builder.Project {
    
    [Serializable]
    [XmlRoot("Project")]
    public class OeProject {
        
        #region static

        private const string XsdName = "Project.xsd";

        public static OeProject Load(string path) {
            OeProject interfaceXml;
            var serializer = new XmlSerializer(typeof(OeProject));
            using (var reader = new StreamReader(path)) {
                interfaceXml = (OeProject) serializer.Deserialize(reader);
            }

            return interfaceXml;
        }

        public static void Save(OeProject xml, string path) {
            var serializer = new XmlSerializer(typeof(OeProject));
            using (TextWriter writer = new StreamWriter(path, false)) {
                serializer.Serialize(writer, xml);
            }
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path) ?? "", XsdName), XsdResources.GetXsdFromResources(XsdName));
        }

        #endregion
        
        [XmlAttribute("noNamespaceSchemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public const string SchemaLocation = XsdName;

        [XmlElement("Properties")]
        public OeProjectProperties Properties { get; set; }
        
        [XmlArray("BuildConfigurations")]
        [XmlArrayItem("Build", typeof(OeBuildConfiguration))]
        public List<OeBuildConfiguration> BuildConfigurations { get; set; }
                
        [XmlElement("AutomatedTasks")]
        public OeAutomatedTasks AutomatedTasks { get; set; }
    }
}