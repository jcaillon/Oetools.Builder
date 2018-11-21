#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeGitFilter.cs) is part of Oetools.Builder.
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
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Attributes;

namespace Oetools.Builder.Project.Properties {
    /// <inheritdoc cref="OeBuildOptions.SourceToBuildGitFilter"/>
    [Serializable]
    public class OeGitFilterOptions : PathListerGitFilterOptions {
            
        /// <inheritdoc cref="PathListerGitFilterOptions.IncludeSourceFilesModifiedSinceLastCommit"/>
        [XmlElement(ElementName = "IncludeSourceFilesModifiedSinceLastCommit")]
        [DefaultValueMethod(nameof(GetDefaultIncludeSourceFilesModifiedSinceLastCommit))]
        public override bool? IncludeSourceFilesModifiedSinceLastCommit { get; set; }

        /// <inheritdoc cref="PathListerGitFilterOptions.IncludeSourceFilesCommittedOnlyOnCurrentBranch"/>
        [XmlElement(ElementName = "IncludeSourceFilesCommittedOnlyOnCurrentBranch")]
        [DefaultValueMethod(nameof(GetDefaultIncludeSourceFilesCommittedOnlyOnCurrentBranch))]
        public override bool? IncludeSourceFilesCommittedOnlyOnCurrentBranch { get; set; }

        /// <inheritdoc cref="PathListerGitFilterOptions.CurrentBranchName"/>
        [XmlElement(ElementName = "CurrentBranchName")]
        public override string CurrentBranchName { get; set; }
        
        /// <inheritdoc cref="PathListerGitFilterOptions.CurrentBranchOriginCommit"/>
        [XmlElement(ElementName = "CurrentBranchOriginCommit")]
        public override string CurrentBranchOriginCommit { get; set; }
    }
}