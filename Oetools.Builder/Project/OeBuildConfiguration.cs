using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Resources;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project {
    
    /// <summary>
    /// Represents the configuration of a build
    /// </summary>
    /// <remarks>
    /// Every public property string not marked with the <see cref="ReplaceVariables"/> attribute is allowed
    /// to use &lt;VARIABLE&gt; which will be replace at the beggining of the build by <see cref="Variables"/>
    /// </remarks>
    [Serializable]
    [XmlRoot("BuildConfiguration")]
    public class OeBuildConfiguration {
                
        #region static

        private const string XsdName = "BuildConfiguration.xsd";

        public static OeBuildConfiguration Load(string path) {
            OeBuildConfiguration interfaceXml;
            var serializer = new XmlSerializer(typeof(OeBuildConfiguration));
            using (var reader = new StreamReader(path)) {
                interfaceXml = (OeBuildConfiguration) serializer.Deserialize(reader);
            }

            return interfaceXml;
        }

        public static void Save(OeBuildConfiguration xml, string path) {
            var serializer = new XmlSerializer(typeof(OeBuildConfiguration));
            using (TextWriter writer = new StreamWriter(path, false)) {
                serializer.Serialize(writer, xml);
            }
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path) ?? "", XsdName), XsdResources.GetXsdFromResources(XsdName));
        }

        #endregion
            
        [XmlAttribute("noNamespaceSchemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public const string SchemaLocation = XsdName;
            
        [XmlAttribute("Name")]
        public string ConfigurationName { get; set; }
        
        [XmlElement("OverloadProperties")]
        [DeepCopy(Ignore = true)]
        public OeProjectProperties Properties { get; set; }
            
        /// <summary>
        /// Default variables are added by the builder, see <see cref="ApplyVariables"/>
        /// </summary>
        [XmlArray("BuildVariables")]
        [XmlArrayItem("Variable", typeof(OeVariable))]
        [ReplaceVariables(SkipReplace = true)]
        public List<OeVariable> Variables { get; set; }
        
        /// <summary>
        /// This list of tasks can include any file
        /// </summary>
        [XmlArray("PreBuildTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> PreBuildTasks { get; set; }

        /// <summary>
        /// This list of tasks can only include files located in the source directory
        /// </summary>
        [XmlArray("BuildSourceTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepCompile))]
        public List<OeBuildStepCompile> BuildSourceTasks { get; set; }
            
        /// <summary>
        /// This list of tasks can only include files located in the output directory
        /// </summary>
        [XmlArray("BuildOutputTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> BuildOutputTasks { get; set; }
        
        /// <summary>
        /// This list of tasks can include any file
        /// </summary>
        [XmlArray("PostBuildTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStepClassic))]
        public List<OeBuildStepClassic> PostBuildTasks { get; set; }
        
        /// <summary>
        /// Set default values for certain properties if they are null
        /// </summary>
        public void SetDefaultValuesWhenNeeded() {
            Properties = Properties ?? new OeProjectProperties();
            Properties.SetDefaultValuesWhenNeeded();
            
            // add a default configuration that build all files next to their respective source file
            BuildSourceTasks = BuildSourceTasks ?? new List<OeBuildStepCompile>();
            if (BuildSourceTasks.Count == 0) {
                BuildSourceTasks.Add(null);
            }
            BuildSourceTasks[0] = BuildSourceTasks[0] ?? new OeBuildStepCompile {
                Label = "Compile all files next to source",
                Tasks = new List<OeTask> {
                    new OeTaskCompile {
                        Include = "((**))*",
                        TargetDirectory = "<1>"
                    }
                }
            };
        }
            
        /// <summary>
        /// Add the default variables and apply the variables on all public properties of type string
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="BuildVariableException"></exception>
        /// <exception cref="BuildConfigurationException"></exception>
        public void ApplyVariables(string sourceDirectory) {
            // add some default variables
            if (!string.IsNullOrEmpty(sourceDirectory)) {
                Variables.Add(new OeVariable { Name = "SOURCE_DIRECTORY", Value = sourceDirectory });    
                Variables.Add(new OeVariable { Name = "PROJECT_DIRECTORY", Value = Path.Combine(sourceDirectory, ".oe") });                
                Variables.Add(new OeVariable { Name = "PROJECT_LOCAL_DIRECTORY", Value = Path.Combine(sourceDirectory, ".oe", "local") });                 
            }             
            Variables.Add(new OeVariable { Name = "DLC", Value = Properties.DlcDirectoryPath });  
            Variables.Add(new OeVariable { Name = "OUTPUT_DIRECTORY", Value = Properties.OutputDirectoryPath });  
            Variables.Add(new OeVariable { Name = "CONFIGURATION_NAME", Value = ConfigurationName });
            try {
                Variables.Add(new OeVariable { Name = "WORKING_DIRECTORY", Value = Directory.GetCurrentDirectory() });
            } catch (Exception e) {
                throw new BuildConfigurationException("Failed to get the current directory (check permissions)", e);
            }
            // extra variable FILE_SOURCE_DIRECTORY defined only when computing targets
            
            // apply variables on variables
            BuilderUtilities.ApplyVariablesInVariables(Variables);
            
            // apply variables in all public string properties
            BuilderUtilities.ApplyVariablesToProperties(this, Variables);  
        }
            
        /// <summary>
        /// Recursively validates that the build configuration is correct
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="BuildConfigurationException"></exception>
        public void ValidateAllTasks() {
            ValidateStepsList(PreBuildTasks, nameof(PreBuildTasks));
            ValidateStepsList(BuildSourceTasks, nameof(BuildSourceTasks));
            ValidateStepsList(BuildOutputTasks, nameof(BuildOutputTasks));
            ValidateStepsList(PostBuildTasks, nameof(PostBuildTasks));
        }

        private void ValidateStepsList(IEnumerable<OeBuildStep> steps, string propertyNameOf) {
            var i = 0;
            foreach (var step in steps) {
                try {
                    if (string.IsNullOrEmpty(step.Label)) {
                        step.Label = $"Step {i}";
                    }
                    step.Validate();
                } catch (Exception e) {
                    var et = e as TaskValidationException;
                    if (et != null) {
                        et.StepNumber = i;
                        et.PropertyName = typeof(OeBuildConfiguration).GetXmlName(propertyNameOf);
                    }
                    throw new BuildConfigurationException(et != null ? et.Message : "Unexpected exception when checking tasks", et ?? e);
                }
                i++;
            }
        }

        public override string ToString() {
            return $"{(string.IsNullOrEmpty(ConfigurationName) ? "unnamed configuration" : $"configuration {ConfigurationName.PrettyQuote()}")}";
        }
    }

}