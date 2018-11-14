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
using Oetools.Utilities.Archive.Prolib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    [Serializable]
    [XmlRoot("Prolib")]
    public class OeTaskFileArchiverArchiveProlib : AOeTaskFileArchiverArchive, IOeTaskWithBuiltFiles {
           
        /// <summary>
        /// The path to the targeted prolib file.
        /// </summary>
        /// <inheritdoc cref="AOeTaskFileArchiverArchive.TargetArchivePath"/>
        [XmlAttribute("TargetProlibFilePath")]
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
        /// The version to use for the pro library file.
        /// </summary>
        [XmlAttribute(AttributeName = "ProlibVersion")]
        public string ProlibVersion { get; set; }
        
        /// <summary>
        /// The openedge codepage to use for file path encoding inside the pro library.
        /// </summary>
        [XmlAttribute(AttributeName = "FilePathCodePage")]
        public string FilePathCodePage { get; set; }

        public ProlibVersion? GetEnumProlibVersion() {
            if (Enum.TryParse(ProlibVersion, true, out ProlibVersion version)) {
                return version;
            }
            Log?.Warn($"Failed to understand the value {ProlibVersion.PrettyQuote()} for {GetType().GetXmlName(nameof(ProlibVersion))}.");
            return null;
        }

        protected override IArchiver GetArchiver() {
            var archiver = Archiver.NewProlibArchiver();
            var version = GetEnumProlibVersion();
            if (version != null) {
                archiver.SetProlibVersion((ProlibVersion) version);
            }
            if (!string.IsNullOrEmpty(FilePathCodePage)) {
                archiver.SetFilePathCodePage(FilePathCodePage);
            }
            return archiver;
        }
        
        protected override AOeTarget GetNewTarget() => new OeTargetProlib();
    }
}