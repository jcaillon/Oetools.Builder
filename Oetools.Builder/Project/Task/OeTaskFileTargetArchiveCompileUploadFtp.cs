#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTargetArchiveCompileUploadFtp.cs) is part of Oetools.Builder.
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
using System.Linq;
using System.Xml.Serialization;
using Oetools.Builder.History;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {
    [Serializable]
    [XmlRoot("CompileUploadFtp")]
    public class OeTaskFileTargetArchiveCompileUploadFtp : OeTaskFileTargetArchiveFtp, IOeTaskCompile {
        
        private OeProperties ProjectProperties { get; set; }

        private List<UoeCompiledFile> CompiledFiles { get; set; }

        public List<UoeCompiledFile> GetCompiledFiles() => CompiledFiles;
        
        public void SetCompiledFiles(List<UoeCompiledFile> compiledFile) {
            CompiledFiles = compiledFile;
        }

        public void SetProperties(OeProperties properties) {
            ProjectProperties = properties;
        }

        protected override void ExecuteForFilesInternal(IEnumerable<IOeFileToBuildTargetArchive> files) {
            var filesToBuild = files.Cast<OeFile>().ToList();
            CompiledFiles = OeTaskCompile.CompileFiles(ProjectProperties, CompiledFiles, ref filesToBuild, CancelSource);
            base.ExecuteForFilesInternal(filesToBuild);
        }
    }
}