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
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// This task adds files into cabinet files.
    /// </summary>
    [Serializable]
    [XmlRoot("Cab")]
    public class OeTaskFileArchiverArchiveCab : AOeTaskFileArchiverArchive, IOeTaskWithBuiltFiles { 
        
        /// <inheritdoc cref="AOeTaskFileArchiverArchive.TargetArchivePath"/>
        [XmlAttribute("TargetCabFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public override string TargetArchivePath { get; set; } 
               
        /// <inheritdoc cref="AOeTaskFileArchiverArchive.TargetFilePath"/>
        [XmlAttribute("RelativeTargetFilePath")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public override string TargetFilePath { get; set; }
        
        /// <inheritdoc cref="AOeTaskFileArchiverArchive.TargetDirectory"/>
        [XmlAttribute("RelativeTargetDirectory")]
        [ReplaceVariables(LeaveUnknownUntouched = true)]
        public override string TargetDirectory { get; set; }      
        
        /// <summary>
        /// The compression level to use for the cabinet file.
        /// </summary>
        [XmlAttribute(AttributeName = "CompressionLevel")]
        public string CompressionLevel { get; set; }

        public ArchiveCompressionLevel? GetEnumCompressionLevel() {
            if (Enum.TryParse(CompressionLevel, true, out ArchiveCompressionLevel level)) {
                return level;
            }
            Log?.Warn($"Failed to understand the value {CompressionLevel.PrettyQuote()} for {GetType().GetXmlName(nameof(CompressionLevel))}.");
            return null;
        }

        protected override IArchiver GetArchiver() {
            var archiver = Archiver.NewCabArchiver();
            var compressionLevel = GetEnumCompressionLevel();
            if (compressionLevel != null) {
                archiver.SetCompressionLevel((ArchiveCompressionLevel) compressionLevel);
            }
            return archiver;
        }
        
        protected override AOeTarget GetNewTarget() => new OeTargetCab();
    }
}