#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeProject.cs) is part of Oetools.Builder.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Resources;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// An openedge project (i.e. an application).
    /// </summary>
    /// <remarks>
    /// A project has:
    /// - properties, that are used to describe your application (for instance, the database needed to compile) and are also used to describe options to build your application (for instance, if the compilation should also generate the xref files).
    /// - variables, that make your build process dynamic. You can use variables almost anywhere in this xml and dynamically overload their values when running the build.
    /// - build configurations, which describe a succession of tasks that build your application. Build configurations can also have their own properties and variables which will overload the ones defined at the project level.
    /// </remarks>
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
            interfaceXml.InitIds();
            return interfaceXml;
        }

        public void Save(string path) {
            var serializer = new XmlSerializer(typeof(OeProject));
            
            XmlDocumentWriter.Save(path, serializer, this);

            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path) ?? "", XsdName), XsdResources.GetXsdFromResources(XsdName));
        }

        #endregion
        
#if USESCHEMALOCATION
        /// <summary>
        /// Only when not generating the build for xsd.exe which has a problem with this attribute
        /// </summary>
        [XmlAttribute("noNamespaceSchemaLocation", Namespace = XmlSchema.InstanceNamespace)]
        public string SchemaLocation = XsdName;
#endif
        
        /// <summary>
        /// A list of build configurations for this project.
        /// </summary>
        /// <remarks>
        /// A build configuration describe how to build your application.
        /// It is essentially a succession of tasks (grouped into steps) that should be carried on in a sequential manner to build your application.
        /// Each build configuration has properties, that are used to describe your application (for instance, the database needed to compile your code) and are also used to describe options to build your application (for instance, if the compilation should also generate the xref files).
        /// Each build configuration can also have variables, that make your build process dynamic. You can use variables almost anywhere in this xml and dynamically overload their values when running the build.
        /// Several build configurations can be defined for a single project.
        /// 
        /// Each build configuration can define "children" build configurations.
        /// Each child inherits its parent properties following these rules:
        /// - If the same leaf is defined for both, the child value is prioritized (leaf = an xml element with no descendant elements).
        /// - If the xml element is a list, the elements if the child (if any) will be added to the elements of the parent (if any). For instance, the tasks list has this behavior.
        /// </remarks>
        [XmlArray("BuildConfigurations")]
        [XmlArrayItem("Configuration", typeof(OeBuildConfiguration))]
        public List<OeBuildConfiguration> BuildConfigurations { get; set; }

        /// <summary>
        /// Returns an initialized project with some initialized properties.
        /// </summary>
        /// <returns></returns>
        public static OeProject GetStandardProject() {
            var output = new OeProject {
                BuildConfigurations = new List<OeBuildConfiguration> {
                    new OeBuildConfiguration {
                        BuildSourceStepGroup = new List<OeBuildStepBuildSource> {
                            new OeBuildStepBuildSource {
                                Name = "Compile all files next to their source",
                                Tasks = new List<AOeTask> {
                                    new OeTaskFileCompile {
                                        Include = "((**))*",
                                        TargetDirectory = "{{1}}"
                                    }
                                }
                            }
                        }
                    }
                }
            };
            return output;
        }

        /// <summary>
        /// Returns a copy of the first build configuration with the given name, or null by default.
        /// </summary>
        /// <param name="configurationName"></param>
        /// <returns></returns>
        public OeBuildConfiguration GetBuildConfigurationCopy(string configurationName) {
            if (configurationName == null) {
                return null;
            }
            
            List<OeBuildConfiguration> buildConfigurationsStack = null;
            
            var configurationsToCheck = new Stack<Tuple<List<OeBuildConfiguration>, List<OeBuildConfiguration>>>();
            configurationsToCheck.Push(new Tuple<List<OeBuildConfiguration>, List<OeBuildConfiguration>>(new List<OeBuildConfiguration>(), BuildConfigurations));
            var found = false;
            while (!found && configurationsToCheck.Count > 0) {
                var parentsAndChildrenList = configurationsToCheck.Pop();
                var parents = parentsAndChildrenList.Item1;
                var children = parentsAndChildrenList.Item2;
                foreach (var child in children) {
                    if (child.Name.Equals(configurationName, StringComparison.CurrentCultureIgnoreCase)) {
                        buildConfigurationsStack = parents;
                        buildConfigurationsStack.Add(child);
                        found = true;
                        break;
                    }
                    if ((child.BuildConfigurations?.Count ?? 0) > 0) {
                        var childrenParents = parents.ToList();
                        childrenParents.Add(child);
                        configurationsToCheck.Push(new Tuple<List<OeBuildConfiguration>, List<OeBuildConfiguration>>(childrenParents, child.BuildConfigurations));
                    }
                }
            }

            if (!found || buildConfigurationsStack.Count < 1) {
                return null;
            }

            var output = buildConfigurationsStack[0].GetDeepCopy();
            for (int i = 1; i < buildConfigurationsStack.Count; i++) {
                buildConfigurationsStack[i].DeepCopy(output);
            }

            return output;
        }

        /// <summary>
        /// Returns the first build configuration found, or null
        /// </summary>
        /// <returns></returns>
        public OeBuildConfiguration GetDefaultBuildConfigurationCopy() {
            return BuildConfigurations?.FirstOrDefault()?.GetDeepCopy() ?? new OeBuildConfiguration();
        }

        /// <summary>
        /// Give everything that has an ID property a unique ID.
        /// </summary>
        internal void InitIds() {
            var buildConfigurationCount = 0;
            var countByPropName = new Dictionary<string, int>();
            
            var objectsToInit = new Queue<Tuple<string, Type, object>>();
            objectsToInit.Enqueue(new Tuple<string, Type, object>(nameof(OeProject), GetType(), this));
            while (objectsToInit.Count > 0) {
                var objectToInit = objectsToInit.Dequeue();
                var objectParentPropName = objectToInit.Item1;
                var objectType = objectToInit.Item2;
                var objectInstance = objectToInit.Item3;
                
                var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var property in properties) {
                    if (!property.CanRead || !property.CanWrite) {
                        continue;
                    }
                    var obj = property.GetValue(objectInstance);
                    if (property.Name.Equals("Id")) {
                        if (objectType.Name.Equals("OeBuildConfiguration")) {
                            // global counter for build configurations id
                            property.SetValue(objectInstance, buildConfigurationCount++);
                            continue;
                        }
                        if (!countByPropName.ContainsKey(objectParentPropName)) {
                            countByPropName.Add(objectParentPropName, 0);
                        }
                        property.SetValue(objectInstance, countByPropName[objectParentPropName]++);
                    } else if (obj is IEnumerable enumerable) {
                        if (property.PropertyType.UnderlyingSystemType.GenericTypeArguments.Length > 0 && property.PropertyType.UnderlyingSystemType.GenericTypeArguments[0] != typeof(string)) {
                            foreach (var item in enumerable) {
                                if (item != null) {
                                    objectsToInit.Enqueue(new Tuple<string, Type, object>($"{objectParentPropName}.{property.Name}", item.GetType(), item));
                                }
                            }
                        }
                    } else if (property.PropertyType.IsClass && property.PropertyType != typeof(string) && obj != null) {
                        objectsToInit.Enqueue(new Tuple<string, Type, object>($"{objectParentPropName}.{property.Name}", property.PropertyType, obj));
                    }
                }
            }
        }
    }
    
}