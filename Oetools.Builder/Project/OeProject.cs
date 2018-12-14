﻿#region header
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
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Resources;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// An Openedge project (typically, your project is your application, they are synonyms).
    /// </summary>
    /// <remarks>
    /// Some facts:
    ///   - A project is composed of one or several build configurations.
    ///   - A build configuration describe how to build your project.
    ///   - To build your project, you need to specify which build configuration to use.
    ///
    /// Below is a simplified representation of a project file:
    ///   Project
    ///   ├─ Build configuration 1
    ///   │  ├─ Child build configuration 2
    ///   │  │  ├─ Child build configuration 3
    ///   │  │  └─ Child build configuration x...
    ///   │  └─ Child build configuration x...
    ///   └─ Build configuration x...
    /// 
    /// There are no upper limit for build configuration nesting.
    /// The idea is that nested build configuration inherit properties from their parent, recursively (more details on the build configuration help).
    /// </remarks>
    [Serializable]
    [XmlRoot("Project")]
    public class OeProject {
        
        #region static

        public const string XsdName = "Project.xsd";

        /// <summary>
        /// Loads a project from a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ProjectException"></exception>
        public static OeProject Load(string path) {
            try {
                OeProject interfaceXml;
                var serializer = new XmlSerializer(typeof(OeProject));
                using (var reader = new StreamReader(path)) {
                    interfaceXml = (OeProject) serializer.Deserialize(reader);
                }
                interfaceXml.InitIds();
                return interfaceXml;
            } catch (Exception e) {
                throw new ProjectException($"An error occured when reading the file {path.PrettyQuote()}: {e.Message}", e);
            }
        }

        /// <summary>
        /// Saves the project to a file.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path) {
            var serializer = new XmlSerializer(typeof(OeProject));
            XmlDocumentWriter.Save(path, serializer, this);
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path) ?? "", XsdName), XsdResources.GetXsdFromResources(XsdName));
        }

        #endregion
        
#if USESCHEMALOCATION
        /// <summary>
        /// Only when not generating the build for xsd.exe which has a problem with this attribute.
        /// </summary>
        [XmlAttribute("noNamespaceSchemaLocation", Namespace = XmlSchema.InstanceNamespace)]
        public string SchemaLocation = XsdName;
#endif

        /// <summary>
        ///  A list of build configurations for this project.
        ///  </summary>
        /// <remarks>
        /// Some facts:
        ///   - A build configuration describe how to build your application.
        ///   - A build configuration is essentially a succession of tasks (grouped into steps) that should be carried on in a sequential manner to build your application.
        ///   - Each build configuration also has properties, which are used to customize the build.
        ///   - Each build configuration can also have variables that make your build process dynamic. You can use variables anywhere in this xml. Their value can be changed dynamically without modifying this xml when running the build.
        ///
        /// Below is a simplified representation of a build configuration:
        ///   Build configuration
        ///   ├─ Variables
        ///   ├─ Properties
        ///   ├─ Build steps
        ///   │  ├─ Step 1
        ///   │  │  ├─ Task 1
        ///   │  │  └─ Task x...
        ///   │  └─ Step x...
        ///   └─ List of children build configuration
        ///   ├─ Child build configuration 1
        ///   └─ Child build configuration x...
        /// 
        /// Inheritance of build configurations (think of russian dolls):
        ///   - Each build configuration can define "children" build configurations.
        ///   - Each child inherits its parent properties following these rules:
        ///   - For a given property, the most nested value is used (i.e. child value prioritized over parent value).
        ///   - If the xml element is a list, the elements of the child (if any) will be added to the elements of the parent (if any). For instance, the steps list has this behavior. This means that the steps of a child will be added to the defined steps of its parents.
        ///
        ///   Practical example:
        ///     Given the project below:
        ///    
        ///     Project
        ///     └─ Build configuration 1
        ///        ├─ Properties
        ///           │  Property1 (x)
        ///        │  └─ Property2
        ///        ├─ Steps
        ///        │  └─ Step1 (x)
        ///        └─ Child build configuration 2
        ///           ├─ Properties
        ///           │  ├─ Property2 (x)
        ///           │  └─ Property3 (x)
        ///           └─ Steps
        ///              └─ Step2 (x)
        ///  
        ///     We decide to build 'Child build configuration 2':
        ///       - Property1 of 'Build configuration 1' will be used.
        ///       - Property2 and Property 3 of 'Build configuration 2' will be used.
        ///       - Two steps will be executed: Step1 from 'Build configuration 1' and Step2 from 'Build configuration 2'.
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
                        BuildSteps = new List<AOeBuildStep> {
                            new OeBuildStepBuildSource {
                                Name = "Source compilation",
                                Tasks = new List<AOeTask> {
                                    new OeTaskFileCompile {
                                        Name = "Compile files next to their source",
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
        /// Returns a copy of the first build configuration with the given name, or null if not found.
        /// </summary>
        /// <param name="configurationName"></param>
        /// <returns></returns>
        public OeBuildConfiguration GetBuildConfigurationCopy(string configurationName) {
            if (configurationName == null) {
                return null;
            }
            if (!int.TryParse(configurationName, out int id)) {
                id = -1;
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
                    if (id >= 0 && child.Id.Equals(id) || (child.Name?.Equals(configurationName, StringComparison.CurrentCultureIgnoreCase) ?? false)) {
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
        /// Returns a flat list of all the configurations in this project.
        /// </summary>
        /// <returns></returns>
        public List<OeBuildConfiguration> GetAllBuildConfigurations() {
            var output = new List<OeBuildConfiguration>(BuildConfigurations);
            var configurationsToCheck = new Stack<List<OeBuildConfiguration>>();
            configurationsToCheck.Push(BuildConfigurations);
            while (configurationsToCheck.Count > 0) {
                var childrenList = configurationsToCheck.Pop();
                foreach (var child in childrenList) {
                    if (child.BuildConfigurations != null && child.BuildConfigurations.Count > 0) {
                        output.AddRange(child.BuildConfigurations);
                        configurationsToCheck.Push(child.BuildConfigurations);
                    }
                }
            }
            return output.OrderBy(b => b.Id).ToList();
        }

        /// <summary>
        /// Returns a copy of a build configuration taking into account inheritance.
        /// </summary>
        /// <param name="buildConfigurationQueue"></param>
        /// <param name="throwIfConfNotFound"></param>
        /// <returns></returns>
        /// <exception cref="ProjectException"></exception>
        public static OeBuildConfiguration GetBuildConfigurationCopy(Queue<Tuple<string, string>> buildConfigurationQueue, bool throwIfConfNotFound = true) {
            if (buildConfigurationQueue.Count == 0) {
                throw new ProjectException("The build configuration stack cannot be empty.");
            }

            var nbConf = -1;
            OeBuildConfiguration rootConf = null;
            OeBuildConfiguration parentConf = null;
            while (buildConfigurationQueue.Count > 0) {
                var tuple = buildConfigurationQueue.Dequeue();
                var proj = Load(tuple.Item1);
                OeBuildConfiguration conf;
                if (string.IsNullOrEmpty(tuple.Item2)) {
                    conf = proj.BuildConfigurations?.FirstOrDefault();
                    if (conf == null && throwIfConfNotFound) {
                        throw new ProjectException($"no build configuration found in the project file {tuple.Item1.PrettyQuote()}.");
                    }
                } else {
                    conf = proj.GetBuildConfigurationCopy(tuple.Item2);
                    if (conf == null && throwIfConfNotFound) {
                        throw new ProjectException($"The build configuration {tuple.Item2.PrettyQuote()} can't be found in the project file {tuple.Item1.PrettyQuote()}.");
                    }
                }
                if (conf == null) {
                    continue;
                }
                conf.Id = ++nbConf;
                if (rootConf == null) {
                    rootConf = conf;
                }
                if (parentConf != null) {
                    parentConf.BuildConfigurations = new List<OeBuildConfiguration> { conf };
                }
                parentConf = conf;
            }

            if (rootConf == null) {
                throw new ProjectException("No build configuration found.");
            }

            return new OeProject {
                BuildConfigurations = new List<OeBuildConfiguration> { rootConf }
            }.GetBuildConfigurationCopy(nbConf.ToString());
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