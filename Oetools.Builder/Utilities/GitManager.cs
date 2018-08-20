#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (GitManager.cs) is part of Oetools.Builder.
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
using System.IO;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Utilities {
    
    public class GitManager {
        
        public GitManager() {
            GitExe = new ProcessIo("git") {
                WorkingDirectory = Directory.GetCurrentDirectory()
            };
        }

        public void SetCurrentDirectory(string path) {
            GitExe.WorkingDirectory = path;
        }

        private ProcessIo GitExe { get; }

        /// <summary>
        /// Returns a list of relative path of all the files that were modified since the last merge commit until now (HEAD)
        /// does not return uncommitted file changes
        /// The last merge commit is the first commit found that has a reference different than the current_branch_name (and different than any ANY_REMOTE/current_branch_name)
        /// </summary>
        /// <param name="optionalBranchOriginCommit">specify the commit from which the current branch starts</param>
        /// <param name="optionalCurrentBranchName"></param>
        /// <exception cref="GitManagerCantFindMergeCommitException">Exception thrown if the commit can't be found</exception>
        /// <returns></returns>
        public List<string> GetAllCommittedFilesExclusiveToCurrentBranch(string optionalBranchOriginCommit = null, string optionalCurrentBranchName = null) {
            var mergeCommit = optionalBranchOriginCommit ?? GetFirstCommitRefNonExclusiveToCurrentBranch(optionalCurrentBranchName);
            if (string.IsNullOrEmpty(mergeCommit)) {
                return new List<string>();
            }
            return ExecuteGitCommand($"diff --name-only HEAD {mergeCommit}")
                .Select(s => s.Trim().StripQuotes())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        /// <summary>
        /// Returns the first commit that has a reference different than the current_branch_name (and different than any ANY_REMOTE/current_branch_name)
        /// To find the current_branch_name, it either uses the HEAD (if a branch is checked out), or it uses the first non tag reference found for the last commit
        /// (if a commit is checked out in detached mode)
        /// </summary>
        /// <exception cref="GitManagerCantFindMergeCommitException">Exception thrown if the commit can't be found</exception>
        /// <returns></returns>
        public string GetFirstCommitRefNonExclusiveToCurrentBranch(string optionalCurrentBranchName = null) {
            List<string> output;
            try {
                output = ExecuteGitCommand("log --pretty=\"format:%H %D\" HEAD");
            } catch (Exception e) {
                if (e.InnerException != null && e.InnerException.Message.Contains("'HEAD'")) {
                    // HEAD doesn't contain any commits
                    throw new GitManagerCantFindMergeCommitException();
                }
                throw;
            }
            if (output == null || output.Count <= 0) {
                throw new GitManagerCantFindMergeCommitException();
            }
            // the first output contains info on the HEAD
            var firstCommitRefs = output[0].Substring(output[0].IndexOf(' ') + 1);
            var firstCommitRef = firstCommitRefs.Split(',')[0];
            string currentBranch = null;
            if (firstCommitRef.Contains("HEAD ->")) {
                // we are on a branch, get the name of the branch commitRef = "HEAD -> v2/ft/INC0439347", we want "v2/ft/INC0439347"
                currentBranch = optionalCurrentBranchName ?? firstCommitRef.Substring(firstCommitRef.IndexOf("-> ", StringComparison.CurrentCultureIgnoreCase) + 3).Trim();
            } else {
                // we are in detached mode commitRef = "HEAD", we are on no branch so obviously we can't find the commit on this non existing branch
                var split = firstCommitRefs.Split(',').ToList();
                for (int j = 1; j < split.Count; j++) {
                    var idx = split[j].IndexOf('/');
                    if (idx > 0 && idx + 1 < split[j].Length && !split[j].StartsWith("tag: ")) {
                        currentBranch = split[j].Substring(idx + 1);
                        break;
                    }
                }
                currentBranch = optionalCurrentBranchName ?? currentBranch;
            }
            if (string.IsNullOrEmpty(currentBranch)) {
                // if we don't know on which we are, we can never find the first merge commit
                throw new GitManagerCantFindMergeCommitException();
            }
            // then we browse every commit and try to deduce if it only belongs to this branch or was merged elsewhere
            int i;
            for (i = 0; i < output.Count; i++) {
                var commitRef = output[i].Substring(output[i].IndexOf(' ') + 1);
                // we can have something like split[1] = "HEAD, tag: v1/rel/package2, tag: v1/rel/package1, origin/v1/rel, origin/v1/dev, v1/rel, v1/ft/INC0439347"
                var references = commitRef.Split(',')
                    .ToList()
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
                // references can be empty if not references for this commit
                bool foundMergeRef = false;
                foreach (var reference in references) {
                    var idx = reference.IndexOf('/');
                    var refWithoutRemote = idx > 0 && idx + 1 < reference.Length ? reference.Substring(idx + 1) : null;
                    if ((refWithoutRemote == null || !refWithoutRemote.Equals(currentBranch)) && !reference.Equals(firstCommitRef)) {
                        // we found a reference that is not the current branch remote, bingo
                        foundMergeRef = true;
                    }
                }
                if (foundMergeRef) {
                    break;
                }
            }
            if (i >= output.Count) {
                // this branch has never been merged, there are not other references in all the branch commits
                throw new GitManagerCantFindMergeCommitException();
            }
            return output[i].Substring(0, output[i].IndexOf(' '));
        }

        /// <summary>
        /// Returns all uncommited file changes in the working copy, including tracked and untracked files
        /// (from working dir and stage area)
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllModifiedFilesSinceLastCommit() {
            return ExecuteGitCommand("status --porcelain=v1 -u")
                .Where(s => !string.IsNullOrEmpty(s) && s.Length >= 3)
                .Select(s => s.Substring(3).Trim().StripQuotes())
                .ToList();
        }

        /// <summary>
        /// Executes a git command
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<string> ExecuteGitCommand(string parameters) {
            if (!GitExe.TryExecute(parameters)) {
                throw new Exception("Error executing a git command", new Exception(GitExe.ErrorOutput.ToString()));
            }
            return GitExe.StandardOutputArray;
        }
    }
}