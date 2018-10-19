#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTargetArchiveDeleteInProlib.cs) is part of Oetools.Builder.
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
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Archive;

namespace Oetools.Builder.Project.Task {
    
    [Serializable]
    [XmlRoot("DeleteInProlib")]
    public class OeTaskFileTargetDeleteInArchiveProlib : OeTaskFileTargetDeleteInArchive, IOeTaskNeedingProperties {
                    
        /// <summary>
        /// The relative file path pattern to delete inside the matched archive.
        /// </summary>
        [XmlAttribute("RelativeFilePatternToDelete")]
        public string RelativeFilePatternToDelete { get; set; }

        protected override string GetRelativeFilePatternToDelete() => RelativeFilePatternToDelete;

        protected override string GetRelativeFilePatternToDeletePropertyName() => nameof(RelativeFilePatternToDelete);

        protected override IArchiver GetArchiver() => Archiver.NewProlibArchiver(GetProperties()?.DlcDirectoryPath);

    }
}