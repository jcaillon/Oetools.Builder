using System.Xml.Serialization;

namespace Oetools.Builder.Utilities {
    /// <summary>
    /// Git based filter.
    /// </summary>
    public abstract class PathListerGitFilterOptions {
        
        /// <summary>
        /// Build source files that were modified/added/deleted since the last commit.
        /// </summary>
        /// <remarks>
        /// This include all the files in staging area as well as untracked files in the working directory. Basically, any file listed in a `git status` command.
        /// This is a good filter to build all the files you are about to commit (and thus check if they compile).
        /// </remarks>
        [XmlIgnore]
        public virtual bool? IncludeSourceFilesModifiedSinceLastCommit { get; set; }
        public static bool GetDefaultIncludeSourceFilesModifiedSinceLastCommit() => false;

        /// <summary>
        /// Build source files that were modified/added/deleted in a commit that is exclusive to the current branch.
        /// </summary>
        /// <remarks>
        /// The `git log` is used to identify which commits are referenced only by the current branch (or reference only by the any remote reference of the current branch). The files modified/added/deleted in these commits are then build.
        /// To rephrase this, we consider the commits from HEAD to the first commit that has a reference different than current_branch or any_remote/current_branch (aforementioned commit is not included).
        /// This option is useful to check if the changes you introduced with several commits in your branch do not break the build of your application.
        /// </remarks>
        [XmlIgnore]
        public virtual bool? IncludeSourceFilesCommittedOnlyOnCurrentBranch { get; set; }
        public static bool GetDefaultIncludeSourceFilesCommittedOnlyOnCurrentBranch() => false;

        /// <summary>
        /// The branch reference name for the current branch. To be used with IncludeSourceFilesCommittedOnlyOnCurrentBranch.
        /// </summary>
        /// <remarks>
        /// This can be useful in CI builds where the CI checks out a repo in detached mode (it checks out a commit, not a branch).
        /// However, by default, if in detached mode, this tool tries to deduce the current branch by checking the first remote reference of the currently checked out commit. This will be sufficient in most cases, so this option can often be left empty.
        /// </remarks>
        [XmlIgnore]
        public virtual string CurrentBranchName { get; set; }

        /// <summary>
        /// The reference or SHA1 of the commit from which the current branch originated. To be used with IncludeSourceFilesCommittedOnlyOnCurrentBranch.
        /// </summary>
        /// <remarks>
        /// Read the description of IncludeSourceFilesCommittedOnlyOnCurrentBranch to understand this option.
        /// This commit is generally found automatically but you can set its value to force a build between HEAD and a given commit.
        /// </remarks>
        [XmlIgnore]
        public virtual string CurrentBranchOriginCommit { get; set; }

        /// <summary>
        /// Is this filter active?
        /// </summary>
        /// <returns></returns>
        internal bool IsActive() => (IncludeSourceFilesCommittedOnlyOnCurrentBranch ?? GetDefaultIncludeSourceFilesCommittedOnlyOnCurrentBranch()) || (IncludeSourceFilesModifiedSinceLastCommit ?? GetDefaultIncludeSourceFilesModifiedSinceLastCommit());
    }
}