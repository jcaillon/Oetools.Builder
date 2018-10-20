﻿#region header
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

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeGitFilterOptions {
            
        /// <summary>
        /// If true, only the files that were modified since the last commit will be eligible for the <see cref="OeBuildConfiguration.BuildSourceStepGroup"/>
        /// (this include files in staging area and untracked files in the working directory)
        /// </summary>
        [XmlElement(ElementName = "OnlyIncludeSourceFilesModifiedSinceLastCommit")]
        public bool? OnlyIncludeSourceFilesModifiedSinceLastCommit { get; set; }
        public static bool GetDefaultOnlyIncludeSourceFilesModifiedSinceLastCommit() => false;
            
        /// <summary>
        /// If true, only the committed files that were committed exclusively on the current branch will be eligible for the <see cref="OeBuildConfiguration.BuildSourceStepGroup"/>
        /// We consider that a commit belongs only to the current branch if we can't find a reference different than CURRENT_BRANCH_NAME and ANY_REMOTE/CURRENT_BRANCH_NAME
        /// which points to said commit
        /// </summary>
        [XmlElement(ElementName = "OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch")]
        public bool? OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch { get; set; }
        public static bool GetDefaultOnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch() => false;
            
        /// <summary>
        /// In detached mode, the CURRENT_BRANCH_NAME is not defined, you can set this value to the branch name to use for the option <see cref="OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch"/>
        /// This can be useful in CI builds where the CI checks out a repo in detached mode (it checks out a commit)
        /// </summary>
        /// <remarks>
        /// By default, if in detached mode, this tool tries to deduce the current branch by checking the first remote reference of the currently checked out commit
        /// </remarks>
        [XmlElement(ElementName = "CurrentBranchName")]
        public string CurrentBranchName { get; set; }
        
        /// <summary>
        /// Manually specify the reference or SHA1 of the commit from which the current branch originated
        /// </summary>
        [XmlElement(ElementName = "CurrentBranchOriginCommit")]
        public string CurrentBranchOriginCommit { get; set; }

        /// <summary>
        /// Is this filter active
        /// </summary>
        /// <returns></returns>
        internal bool IsActive() => (OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch ?? GetDefaultOnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch()) || (OnlyIncludeSourceFilesModifiedSinceLastCommit ?? GetDefaultOnlyIncludeSourceFilesModifiedSinceLastCommit());
    }
}