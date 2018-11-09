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
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.History;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Archive;

namespace Oetools.Builder.Project.Task {
    
    [Serializable]
    [XmlRoot("Cab")]
    public class OeTaskFileArchiverArchiveCab : AOeTaskFileArchiverArchive, IOeTaskWithBuiltFiles {
        
        /// <summary>
        /// Relative path inside the archive.
        /// </summary>
        [XmlAttribute("RelativeTargetFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string RelativeTargetFilePath { get; set; }
        
        /// <summary>
        /// Relative path inside the archive.
        /// </summary>
        [XmlAttribute("RelativeTargetDirectory")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string RelativeTargetDirectory { get; set; }       
        
        [XmlAttribute("TargetCabFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public string TargetCabFilePath { get; set; }
        
        [XmlAttribute(AttributeName = "ArchivesCompressionLevel")]
        public string ArchivesCompressionLevel { get; set; }

        public override ArchiveCompressionLevel GetCompressionLevel() {
            if (Enum.TryParse(ArchivesCompressionLevel, true, out ArchiveCompressionLevel level)) {
                return level;
            }
            return ArchiveCompressionLevel.None;
        }

        protected override IArchiver GetArchiver() => Archiver.NewCabArchiver();
        
        protected override AOeTarget GetNewTarget() => new OeTargetCab();

        protected override string GetArchivePath() => TargetCabFilePath;

        protected override string GetArchivePathPropertyName() => nameof(TargetCabFilePath);

        protected override string GetTargetFilePath() => RelativeTargetFilePath;

        protected override string GetTargetFilePathPropertyName() => nameof(RelativeTargetFilePath);

        protected override string GetTargetDirectory() => RelativeTargetDirectory;

        protected override string GetTargetDirectoryPropertyName() => nameof(RelativeTargetDirectory);
    }
    
    [Serializable]
    public enum OeCompressionLevel {
        [XmlEnum("None")] None,
        [XmlEnum("Max")] Max
    }
}