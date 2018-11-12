#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTargetArchiveCab.cs) is part of Oetools.Builder.
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
using System.Xml.Serialization;
using Oetools.Builder.History;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Archive;

namespace Oetools.Builder.Project.Task {
    
    [Serializable]
    [XmlRoot("Copy")]
    public class OeTaskFileCopy : AOeTaskFileArchiverArchive, IOeTaskWithBuiltFiles {
        
        /// <summary>
        /// Target file path.
        /// </summary>
        [XmlAttribute("TargetFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetFilePath { get; set; }
        
        /// <summary>
        /// Target directory.
        /// </summary>
        [XmlAttribute("TargetDirectory")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetDirectory { get; set; }       
        
        protected override IArchiver GetArchiver() => Archiver.NewFileSystemArchiver();
        
        protected override AOeTarget GetNewTarget() => new OeTargetFile();

        protected override string GetArchivePath() => null;

        protected override string GetArchivePathPropertyName() => null;

        protected override string GetTargetFilePath() => TargetFilePath;

        protected override string GetTargetFilePathPropertyName() => nameof(TargetFilePath);

        protected override string GetTargetDirectory() => TargetDirectory;

        protected override string GetTargetDirectoryPropertyName() => nameof(TargetDirectory);
    }
}