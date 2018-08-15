﻿using System;
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
        [ReplaceVariables(SkipReplace = true)]
        public string ConfigurationName { get; set; }
        
        [XmlElement("OverloadProjectProperties")]
        [DeepCopy(Ignore = true)]
        public OeProjectProperties Properties { get; set; }
            
        /// <summary>
        /// Default variables are added by the builder, see <see cref="Builder.AddDefaultVariables"/>
        /// </summary>
        [XmlArray("BuildVariables")]
        [XmlArrayItem("Variable", typeof(OeVariable))]
        [ReplaceVariables(SkipReplace = true)]
        public List<OeVariable> Variables { get; set; }
            
        [XmlElement(ElementName = "OutputDirectory")]
        public string OutputDirectory { get; set; } = Path.Combine("<SOURCE_DIRECTORY>", "bin");

        [XmlElement(ElementName = "ReportFilePath")]
        public string ReportFilePath { get; set; } = Path.Combine("<PROJECT_DIRECTORY>", "build", "latest.html");
            
        [XmlElement(ElementName = "BuildHistoryOutputFilePath")]
        public string BuildHistoryOutputFilePath { get; set; } = Path.Combine("<PROJECT_DIRECTORY>", "build", "latest.xml");
            
        [XmlElement(ElementName = "BuildHistoryInputFilePath")]
        public string BuildHistoryInputFilePath { get; set; } = Path.Combine("<PROJECT_DIRECTORY>", "build", "latest.xml");
                                    
        [XmlElement("CompilationOptions")]
        public OeCompilationOptions CompilationOptions { get; set; }
            
        [XmlElement("BuildOptions")]
        public OeBuildOptions BuildOptions { get; set; }
            
        /// <summary>
        /// Allows to exclude path from being treated by <see cref="BuildSourceTasks"/>
        /// </summary>
        [XmlArray("SourcePathFilters")]
        [XmlArrayItem("Filter", typeof(OeFilter))]
        [XmlArrayItem("FilterRegex", typeof(OeFilterRegex))]
        public List<OeFilter> SourcePathFilters { get; set; }
        
        /// <summary>
        /// This list of tasks can include any file
        /// </summary>
        [XmlArray("PreBuildTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStep))]
        public List<OeBuildStep> PreBuildTasks { get; set; }

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
        [XmlArrayItem("Step", typeof(OeBuildStep))]
        public List<OeBuildStep> BuildOutputTasks { get; set; }
        
        /// <summary>
        /// This list of tasks can include any file
        /// </summary>
        [XmlArray("PostBuildTasks")]
        [XmlArrayItem("Step", typeof(OeBuildStep))]
        public List<OeBuildStep> PostBuildTasks { get; set; }
            
        [Serializable]
        public class OeCompilationOptions {

            [XmlElement(ElementName = "CompileWithDebugList")]
            public bool CompileWithDebugList { get; set; }

            [XmlElement(ElementName = "CompileWithXref")]
            public bool CompileWithXref { get; set; }

            [XmlElement(ElementName = "CompileWithListing")]
            public bool CompileWithListing { get; set; }

            [XmlElement(ElementName = "CompileWithPreprocess")]
            public bool CompileWithPreprocess { get; set; }

            [XmlElement(ElementName = "UseCompilerMultiCompile")]
            public bool UseCompilerMultiCompile { get; set; }

            /// <summary>
            /// only since 11.7 : require-full-names, require-field-qualifiers, require-full-keywords
            /// </summary>
            [XmlElement(ElementName = "CompileOptions")]
            public bool CompileOptions { get; set; }

            /// <summary>
            /// Force the usage of a temporary Directory to compile the .r code files
            /// </summary>
            [XmlElement(ElementName = "CompileForceUsageOfTemporaryDirectory")]
            public bool CompileForceUsageOfTemporaryDirectory { get; set; }

            [XmlElement(ElementName = "CompilableFilePattern")]
            public string CompilableFilePattern { get; set; }
                
            [XmlElement(ElementName = "CompileForceSingleProcess")]
            public bool CompileForceSingleProcess { get; set; }

            [XmlElement(ElementName = "CompileNumberProcessPerCore")]
            public byte CompileNumberProcessPerCore { get; set; }

            [XmlElement(ElementName = "CompileMinimumNumberOfFilesPerProcess")]
            public int CompileMinimumNumberOfFilesPerProcess { get; set; }
            
            [XmlElement(ElementName = "UseSimplerAnalysisForDatabaseReference")]
            public bool UseSimplerAnalysisForDatabaseReference { get; set; }
        }
            
        [Serializable]
        public class OeBuildOptions {
                
            /// <summary>
            /// If false, there will be no analyze of compiled files (ref tables/files), no storage
            /// of the build history after the build, no computation of MD5 nor comparison of date/size of files
            /// </summary>
            [XmlElement(ElementName = "EnableDifferentialBuild")]
            public bool EnableDifferentialBuild { get; set; }
                
            /// <summary>
            /// True if the tool should use a MD5 sum for each file to figure out if it has changed
            /// </summary>
            [XmlElement(ElementName = "StoreSourceMd5")]
            public bool StoreSourceMd5 { get; set; }
            
            /// <summary>
            /// If a source file has been deleted since the last build, should we try to delete it in the output directory
            /// if it still exists?
            /// </summary>
            [XmlElement(ElementName = "MirrorDeletedSourceFileToOutput")]
            public bool MirrorDeletedSourceFileToOutput { get; set; }
        }

        public void Validate() {
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
    }

}