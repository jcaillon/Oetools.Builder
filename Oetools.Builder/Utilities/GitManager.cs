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
        /// Returns a list of relative path of all the files that were modified since the given commit until now (HEAD)
        /// does not return uncommitted file changes
        /// </summary>
        /// <exception cref="GitManagerSingleBranchException"></exception>
        /// <returns></returns>
        public List<string> GetAllCommittedFilesModifiedSinceLastMerge(string optionalCurrentBranchName = null) {
            var mergeCommit = GetFirstMergedCommitRefOnCurrentBranch(optionalCurrentBranchName);
            if (string.IsNullOrEmpty(mergeCommit)) {
                return new List<string>();
            }
            return ExecuteGitCommand($"diff --name-only HEAD {mergeCommit}")
                .Select(s => s.Trim().StripQuotes())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        /// <summary>
        /// From the current HEAD, returns the first commit that was not merged in another branch (the remote of this branch don't count)
        /// </summary>
        /// <exception cref="GitManagerSingleBranchException"></exception>
        /// <returns></returns>
        public string GetFirstMergedCommitRefOnCurrentBranch(string optionalCurrentBranchName = null) {
            List<string> output;
            try {
                output = ExecuteGitCommand("log --pretty=\"format:%H %D\" HEAD");
            } catch (Exception e) {
                if (e.InnerException != null && e.InnerException.Message.Contains("'HEAD'")) {
                    // HEAD doesn't contain any commits
                    throw new GitManagerSingleBranchException();
                }
                throw;
            }
            if (output == null || output.Count <= 0) {
                throw new GitManagerSingleBranchException();
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
                if (string.IsNullOrEmpty(optionalCurrentBranchName)) {
                    var split = firstCommitRefs.Split(',').ToList();
                    for (int j = 1; j < split.Count; j++) {
                        var idx = split[j].IndexOf('/');
                        if (idx > 0 && idx + 1 < split[j].Length && !split[j].StartsWith("tag: ")) {
                            currentBranch = split[j].Substring(idx + 1);
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(currentBranch)) {
                        // otherwise we will just list all committed files in the repo
                        throw new GitManagerSingleBranchException();
                    }
                } else {
                    currentBranch = optionalCurrentBranchName;
                }
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
                throw new GitManagerSingleBranchException();
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