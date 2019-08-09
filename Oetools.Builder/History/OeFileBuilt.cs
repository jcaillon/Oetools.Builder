#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeFileBuilt.cs) is part of Oetools.Builder.
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
using System.Collections.Generic;
using System.Xml.Serialization;
using DotUtilities;
using DotUtilities.Extensions;
using Oetools.Builder.Utilities.Attributes;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeFileBuilt : OeFile, IOeFileBuilt {

        public OeFileBuilt() { }

        public OeFileBuilt(IOeFile sourceFile) {
            sourceFile.DeepCopy(this);
        }

        public OeFileBuilt(IOeFileBuilt sourceFile) {
            sourceFile.DeepCopy(this);
        }

        /// <summary>
        /// Deep copy the file but with different targets.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targets"></param>
        public OeFileBuilt(IOeFileBuilt sourceFile, List<AOeTarget> targets) {
            var savedTargets = sourceFile.Targets;
            sourceFile.Targets = targets;
            sourceFile.DeepCopy(this);
            sourceFile.Targets = savedTargets;
        }

        /// <inheritdoc />
        [XmlArray("Targets")]
        [XmlArrayItem("Cab", typeof(OeTargetCab))]
        [XmlArrayItem("Copy", typeof(OeTargetFile))]
        [XmlArrayItem("Ftp", typeof(OeTargetFtp))]
        [XmlArrayItem("Prolib", typeof(OeTargetProlib))]
        [XmlArrayItem("Zip", typeof(OeTargetZip))]
        public List<AOeTarget> Targets { get; set; }

        /// <inheritdoc />
        [XmlArray("RequiredFiles")]
        [XmlArrayItem("RequiredFile", typeof(string))]
        [BaseDirectory(Type = BaseDirectoryType.SourceDirectory)]
        public List<string> RequiredFiles { get; set; }

        /// <inheritdoc />
        [XmlArray("RequiredDatabaseReferences")]
        [XmlArrayItem("Table", typeof(OeDatabaseReferenceTable))]
        [XmlArrayItem("Sequence", typeof(OeDatabaseReferenceSequence))]
        public List<OeDatabaseReference> RequiredDatabaseReferences { get; set; }

        /// <inheritdoc />
        [XmlArray("CompilationProblems")]
        [XmlArrayItem("Error", typeof(OeCompilationError))]
        [XmlArrayItem("Warning", typeof(OeCompilationWarning))]
        public List<AOeCompilationProblem> CompilationProblems { get; set; }

        /// <summary>
        /// Takes several files built and returns a <see cref="PathList{T}"/>. If a file is defined several times, its targets are merged into the final file returned.
        /// </summary>
        /// <param name="filesBuilt"></param>
        /// <returns></returns>
        public static PathList<IOeFileBuilt> MergeBuiltFilesTargets(IEnumerable<IOeFileBuilt> filesBuilt) {

            var output = new PathList<IOeFileBuilt>();

            foreach (var fileBuilt in filesBuilt) {
                var historyFileBuilt = output[fileBuilt];
                if (historyFileBuilt == null) {
                    historyFileBuilt = new OeFileBuilt();
                    fileBuilt.DeepCopy<IOeFile>(historyFileBuilt);
                    output.Add(historyFileBuilt);
                }

                // append targets
                if (fileBuilt.Targets != null && fileBuilt.Targets.Count > 0) {
                    (historyFileBuilt.Targets ?? (historyFileBuilt.Targets = new List<AOeTarget>())).AddRange(fileBuilt.Targets);
                }

                // set other properties if they do not exist yet
                if (historyFileBuilt.RequiredFiles == null) {
                    historyFileBuilt.RequiredFiles = fileBuilt.RequiredFiles;
                }
                if (historyFileBuilt.RequiredDatabaseReferences == null) {
                    historyFileBuilt.RequiredDatabaseReferences = fileBuilt.RequiredDatabaseReferences;
                }
                if (historyFileBuilt.CompilationProblems == null) {
                    historyFileBuilt.CompilationProblems = fileBuilt.CompilationProblems;
                }
            }

            return output;
        }
    }
}
