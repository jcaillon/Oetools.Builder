using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    public class OeGitFilter {
            
        /// <summary>
        /// If true, only the files that were modified since the last commit will be elligible for the <see cref="OeBuildConfiguration.BuildSourceTasks"/>
        /// (this include files in staging area and untracked files in the working directory)
        /// </summary>
        [XmlElement(ElementName = "OnlyIncludeSourceFilesModifiedSinceLastCommit")]
        public bool? OnlyIncludeSourceFilesModifiedSinceLastCommit { get; set; }
            
        /// <summary>
        /// If true, only the committed files that were committed exclusively on the current branch will be elligible for the <see cref="OeBuildConfiguration.BuildSourceTasks"/>
        /// We consider that a commit belongs only to the current branch if we can't find a reference different than CURRENT_BRANCH_NAME and ANY_REMOTE/CURRENT_BRANCH_NAME
        /// which points to said commit
        /// </summary>
        [XmlElement(ElementName = "OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch")]
        public bool? OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch { get; set; }
            
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
    }
}