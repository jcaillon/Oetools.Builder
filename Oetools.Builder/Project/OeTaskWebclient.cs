﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.History;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeTaskWebclient : OeTask, ITaskExecute {
        
        [XmlElement(ElementName = "VendorName")]
        public string VendorName { get; set; }

        [XmlElement(ElementName = "ApplicationName")]
        public string ApplicationName { get; set; }

        /// <summary>
        /// defaults to ApplicationName + autoincremented webclient version
        /// </summary>
        [XmlElement(ElementName = "ApplicationVersion")]
        public string ApplicationVersion { get; set; }

        [XmlElement(ElementName = "StartupParameters")]
        public string StartupParameters { get; set; }

        /// <summary>
        /// Will be used for both Prowcapp and Codebase by default, provide a custom prowcapp template to change this behavior
        /// </summary>
        [XmlElement(ElementName = "LocatorUrl")]
        public string LocatorUrl { get; set; }

        /// <summary>
        /// Valid oe version for this application
        /// </summary>
        [XmlElement(ElementName = "WebClientVersion")]
        public string WebClientVersion { get; set; } = "11.7";

        /// <summary>
        /// The Directory path from which to create the webclient files (can be relative to the build output directory, the default value is ".")
        /// </summary>
        [XmlElement(ElementName = "WebclientRootDirectoryPath")]
        public string WebclientRootDirectoryPath { get; set; } = ".";

        /// <summary>
        /// The output directory in which the .prowcapp and .cab + diffs/.cab files will be created (can be relative to the build output directory)
        /// </summary>
        [XmlElement(ElementName = "WebclientOutputDirectory")]
        public string WebclientOutputDirectory { get; set; } = "webclient";

        /// <summary>
        /// Path to the model of the .prowcapp to use (can be left empty and the internal model will be used)
        /// </summary>
        [XmlElement(ElementName = "ProwcappTemplateFilePath")]
        public string ProwcappTemplateFilePath { get; set; }
            
        /// <summary>
        /// If null, all the files in the root path will be added to a default component named as <see cref="ApplicationVersion"/>
        /// </summary>
        [XmlArray("Components")]
        [XmlArrayItem("Component", typeof(OeWebclientComponent))]
        public List<OeWebclientComponent> Components { get; set; }

        [Serializable]
        public class OeWebclientComponent {
                
            [XmlAttribute(AttributeName = "DownloadMode")]
            public OeWebclientComponentDownloadMode DownloadMode { get; set; }
                            
            [XmlArray("IncludedFiles")]
            [XmlArrayItem("IncludePathPattern", typeof(string))]
            public List<string> IncludedFiles { get; set; }
                
            [Serializable]
            public enum OeWebclientComponentDownloadMode {
                [XmlEnum("Eager")] 
                Eager,
                [XmlEnum("Lazy")] 
                Lazy
            }
        }
        
        public void Execute() => throw new NotImplementedException();
        
        public OeWebclientPackage GetWebclientPackageResult() => throw new NotImplementedException();
            
        internal class DiffCab {
            /// <summary>
            ///     1
            /// </summary>
            internal int VersionToUpdateFrom { get; set; }

            /// <summary>
            ///     2
            /// </summary>
            internal int VersionToUpdateTo { get; set; }

            /// <summary>
            ///     $TARGET/wcp/new-10-02/diffs/new1to2.cab
            /// </summary>
            internal string CabPath { get; set; }

            /// <summary>
            ///     $REFERENCE/wcp/new-10-02/diffs/new1to2.cab
            /// </summary>
            internal string ReferenceCabPath { get; set; }

            /// <summary>
            ///     $TARGET/wcp/new-10-02/diffs/new1to2
            /// </summary>
            internal string TempCabFolder { get; set; }

            /// <summary>
            ///     List of all the files that were deployed in the clientNWK since this VersionToUpdateFrom
            /// </summary>
            internal List<OeFileBuilt> FilesDeployedInNwkSincePreviousVersion { get; set; }

        }
    }
}