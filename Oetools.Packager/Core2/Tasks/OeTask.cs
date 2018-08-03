// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTask.cs) is part of Oetools.Packager.
// 
// Oetools.Packager is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Packager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Packager. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

using System;
using System.Collections.Generic;

namespace Oetools.Packager.Core.Tasks {
    public abstract class OeTask { }

    public class OeTaskVariable {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    public class OeSourceFilter {
        public string Exclude { get; set; }
    }

    public abstract class OeTaskOnFile : OeTask {

        internal virtual void ExecuteForFile(string filePath) { }

        public string Include { get; set; }

        public string Exclude { get; set; }
    }

    public abstract class OeTaskOnFileWithTarget : OeTaskOnFile {
        public virtual string Target { get; set; }
    }

    public class OeTaskCopy : OeTaskOnFileWithTarget { }

    public class OeTaskCompile : OeTaskCopy { }

    public class OeTaskMove : OeTaskOnFileWithTarget { }

    public class OeTaskExec : OeTask {

        internal void Execute() {
            // TODO
        }

        public string ExecuablePath { get; set; }

        /// <summary>
        /// (you can use task variables in this string)
        /// </summary>
        public string Parameters { get; set; }

        public bool HiddenExecution { get; set; }

        /// <summary>
        /// With this option, the task will not fail if the exit code is different of 0
        /// </summary>
        public bool IgnoreExitCode { get; set; }

        /// <summary>
        /// (default to output directory)
        /// </summary>
        public string WorkingDirectory { get; set; }
    }

    public class OeTaskDelete : OeTaskOnFile { }

    public class OeTaskRemoveDir : OeTaskOnFile { }

    public class OeTaskDeleteInProlib : OeTaskOnFile {
        /// <summary>
        /// The relative file path pattern to delete inside the matched prolib file
        /// </summary>
        public string RelativeFilePatternToDelete { get; set; }
    }

    public class OeTaskProlib : OeTaskOnFileWithTarget {
        public override string Target { get; set; }
    }

    public class OeTaskZip : OeTaskOnFileWithTarget {
        public override string Target { get; set; }
    }

    public class OeTaskCab : OeTaskOnFileWithTarget {
        public override string Target { get; set; }
    }

    public class OeTaskFtp : OeTaskOnFileWithTarget {
        public override string Target { get; set; }
    }

    public class XmlOeFileInfo {
        /// <summary>
        /// The relative path of the source file
        /// </summary>
        public string SourcePath { get; set; }

        public DateTime LastWriteTime { get; set; }

        public long Size { get; set; }

        /// <summary>
        ///     MD5
        /// </summary>
        public string Md5 { get; set; }
    }

    public class XmlOeBuiltFile : XmlOeFileInfo {
        /// <summary>
        /// Represents the state of the file for this build compare to the previous one
        /// </summary>
        public XmlOeFileState State { get; set; }

        /// <summary>
        /// A list of the targets for this file
        /// </summary>
        public List<XmlOeTarget> Targets { get; set; }
    }

    public class XmlOeBuiltFileCompiled : XmlOeBuiltFile {

        /// <summary>
        /// Represents the source file (i.e. includes) used to generate a given .r code file
        /// </summary>
        public List<XmlOeFileInfo> RequiredFiles { get; set; }

        /// <summary>
        ///     represent the tables that were referenced in a given .r code file
        /// </summary>
        public List<XmlTableCrc> RequiredTables { get; set; }
    }

    public class XmlTableCrc {
        public string QualifiedTableName { get; set; }
            
        public string Crc { get; set; }
    }
    
    public enum XmlOeFileState {
        Added,
        Replaced,
        Deleted,
        Existing
    }

    public abstract class XmlOeTarget {
        /// <summary>
        /// Relative target path (relative to the target directory)
        /// </summary>
        public string TargetPath { get; set; }
    }

    public class XmlOeTargetCompile : XmlOeTarget { }

    public class XmlOeTargetCopy : XmlOeTarget { }

    public abstract class XmlOeTargetPack : XmlOeTarget {
        /// <summary>
        /// Relative path of the pack in which this file is deployed (if any)
        /// </summary>
        public string TargetPack { get; set; }
    }

    public class XmlOeTargetProlib : XmlOeTargetPack {
    }

    public class XmlOeTargetZip : XmlOeTargetPack {
    }

    public class XmlOeTargetCab : XmlOeTargetPack {
    }

}