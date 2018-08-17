#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (FileLister.cs) is part of Oetools.Builder.
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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Utilities {
    
    public class SourceFilesLister {
        
        protected ILogger Log { get; set; }
        
        public string SourceDirectory { get; }

        public List<OeFilter> SourcePathFilters { get; set; }

        /// <summary>
        /// If true, we consider that 2 files are different if they have different hash results
        /// </summary>
        /// <remarks>
        /// by default, we consider the file size to see if they are different
        /// </remarks>
        public bool UseHashComparison { get; set; }

        /// <summary>
        /// if true, we consider that 2 files are different if they have different <see cref="FileSystemInfo.LastWriteTime"/>
        /// </summary>
        /// <remarks>
        /// by default, we consider the file size to see if they are different
        /// </remarks>
        public bool UseLastWriteDateComparison { get; set; } = true;

        public OeGitFilter SourcePathGitFilter { get; set; }

        public List<OeFile> PreviousSourceFiles { get; set; }

        public SourceFilesLister(string sourceDirectory) {
            SourceDirectory = sourceDirectory.ToCleanPath();
        }

        /// <summary>
        /// Returns a list of files in the <see cref="SourceDirectory"/>, considering all the filter options in this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<OeFile> GetFileList() {
            HashSet<string> listedFiles;
            if (SourcePathGitFilter != null && ((SourcePathGitFilter.OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch ?? false) || (SourcePathGitFilter.OnlyIncludeSourceFilesModifiedSinceLastCommit ?? false))) {
                listedFiles = GetBaseFileListFromGit();
            } else {
                listedFiles = GetBaseFileList();
            }

            var output = new List<OeFile>();
            foreach (var file in listedFiles) {
                var oeFile = new OeFile(file); // all the files here exist
                // get info on each files
                SetFileBaseInfo(oeFile);
                // get the state of each file (added/unchanged/modified)
                SetFileState(oeFile);
                output.Add(oeFile);
            }
            
            // add all previous source files that are now missing
            output.AddRange(GetDeletedFileList(listedFiles));
            return output;
        }

        /// <summary>
        /// Returns a list of deleted OeFile based on the <see cref="PreviousSourceFiles"/> and <param name="currentFileList"></param>
        /// which represents the files currently existing
        /// </summary>
        /// <param name="currentFileList"></param>
        /// <returns></returns>
        private List<OeFile> GetDeletedFileList(HashSet<string> currentFileList) {
            List<OeFile> output = new List<OeFile>();
            if (PreviousSourceFiles == null) {
                return output;
            }
            foreach (var previousSourceFile in PreviousSourceFiles.Where(f => f.State != OeFileState.Deleted)) {
                if (!currentFileList.Contains(previousSourceFile.SourcePath)) {
                    var deletedFile = previousSourceFile.GetDeepCopy();
                    deletedFile.State = OeFileState.Deleted;
                    output.Add(deletedFile);
                }
            }
            return output;
        }

        /// <summary>
        /// Sets the <see cref="OeFile.State"/> of a file
        /// </summary>
        /// <param name="oeFile"></param>
        /// <exception cref="Exception"></exception>
        private void SetFileState(OeFile oeFile) {
            if (PreviousSourceFiles == null) {
                oeFile.State = OeFileState.Added;
                return;
            }
            // TODO : might be worth it to turn PreviousSourceFiles into a dictionnary?
            var previousFile = PreviousSourceFiles.FirstOrDefault(f => f.SourcePath.EqualsCi(oeFile.SourcePath));
            if (previousFile == null || previousFile.State == OeFileState.Deleted) {
                oeFile.State = OeFileState.Added;
            } else {
                if (HasFileChanged(previousFile, oeFile)) {
                    oeFile.State = OeFileState.Modified;
                } else {
                    oeFile.State = OeFileState.Unchanged;
                    oeFile.Hash = previousFile.Hash ?? oeFile.Hash;
                }
            }
        }

        /// <summary>
        /// Has the file changed since <param name="previous" /> version to <param name="now" /> version
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool HasFileChanged(OeFile previous, OeFile now) {
            // test if it has changed
            if (previous.Size.Equals(now.Size)) {
                if (!UseLastWriteDateComparison || previous.LastWriteTime.Equals(now.LastWriteTime)) {
                    if (!UseHashComparison || !string.IsNullOrEmpty(previous.Hash) && previous.Hash.Equals(SetFileHash(now).Hash, StringComparison.OrdinalIgnoreCase)) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Sets the <see cref="OeFile.Hash"/> of a file if needed
        /// </summary>
        /// <param name="oeFile"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static OeFile SetFileHash(OeFile oeFile) {
            if (!string.IsNullOrEmpty(oeFile.Hash)) {
                return oeFile;
            }
            try {
                using (var md5 = MD5.Create()) {
                    using (var stream = File.OpenRead(oeFile.SourcePath)) {
                        StringBuilder sBuilder = new StringBuilder();
                        foreach (var b in md5.ComputeHash(stream)) {
                            sBuilder.Append(b.ToString("x2"));
                        }
                        // Return the hexadecimal string
                        oeFile.Hash = sBuilder.ToString();
                    }
                }
            } catch (Exception e) {
                throw new Exception($"Error getting information on file {oeFile.SourcePath}, check permissions", e);
            }
            return oeFile;
        }

        /// <summary>
        /// Sets the <see cref="OeFile.Size"/> and <see cref="OeFile.LastWriteTime"/> of a file if needed
        /// </summary>
        /// <param name="oeFile"></param>
        /// <exception cref="Exception"></exception>
        private static void SetFileBaseInfo(OeFile oeFile) {
            if (oeFile.Size > 0) {
                return;
            }
            try {
                var fileInfo = new FileInfo(oeFile.SourcePath);
                oeFile.Size = fileInfo.Length;
                oeFile.LastWriteTime = fileInfo.LastWriteTime;
            } catch (Exception e) {
                throw new Exception($"Error getting information on file {oeFile.SourcePath}, check permissions", e);
            }
        }

        /// <summary>
        /// Returns a list of all the files in the source directory, filtered with <see cref="SourcePathFilters"/>
        /// </summary>
        /// <returns></returns>
        private HashSet<string> GetBaseFileList() {
            var sourcePathExcludeRegexStrings = OeFilter.GetExclusionRegexStringsFromFilters(SourcePathFilters, SourceDirectory);
            return Utils.EnumerateAllFiles(SourceDirectory, SearchOption.AllDirectories, sourcePathExcludeRegexStrings)
                .ToHashSet(new HashSet<string>(StringComparer.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Returns a list of files in the source directory based on a git filter and also filtered with <see cref="SourcePathFilters"/>
        /// </summary>
        /// <returns></returns>
        private HashSet<string> GetBaseFileListFromGit() {
            var output = new List<string>();

            var gitManager = new GitManager();
            gitManager.SetCurrentDirectory(SourceDirectory);
            
            if (SourcePathGitFilter.OnlyIncludeSourceFilesModifiedSinceLastCommit ?? false) {
                output.AddRange(gitManager.GetAllModifiedFilesSinceLastCommit());
            }
            
            if (SourcePathGitFilter.OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch ?? false) {
                try {
                    output.AddRange(gitManager.GetAllCommittedFilesExclusiveToCurrentBranch(SourcePathGitFilter.CurrentBranchOriginCommit, SourcePathGitFilter.CurrentBranchName));
                } catch (GitManagerCantFindMergeCommitException) {
                    // this exception means we can't find commits that exist only on that branch
                    // we list every file committed in the repo instead (= all files in repo minus all modified files)
                    var allFiles = GetBaseFileList();
                    var workingFiles = gitManager.GetAllModifiedFilesSinceLastCommit().ToHashSet(new HashSet<string>(StringComparer.CurrentCultureIgnoreCase));
                    output.AddRange(allFiles.Select(f => f.FromAbsolutePathToRelativePath(SourceDirectory)).Where(f => !workingFiles.Contains(f)));
                }
            }
            
            var sourcePathExcludeRegexStrings = OeFilter.GetExclusionRegexStringsFromFilters(SourcePathFilters, null, null)?.Select(r => new Regex(r)).ToList();
            
            // Git returns relative path, convert them into absolute path, 
            // it can also return deleted files, keep only existing files
            // finally, apply the exclusion filter
            return output
                .Select(s => Path.Combine(SourceDirectory, s.ToCleanPath()))
                .Where(File.Exists)
                .Where(f => sourcePathExcludeRegexStrings == null || sourcePathExcludeRegexStrings.All(r => !r.IsMatch(f)))
                .ToHashSet(new HashSet<string>(StringComparer.CurrentCultureIgnoreCase));
        }
        
        /// <summary>
        /// Filter the list of files with the <see cref="SourcePathFilters"/>
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public IEnumerable<OeFile> FilterSourceFiles(IEnumerable<OeFile> files) {
            var sourcePathExcludeRegexStrings = OeFilter.GetExclusionRegexStringsFromFilters(SourcePathFilters, SourceDirectory)?.Select(r => new Regex(r)).ToList();
            return sourcePathExcludeRegexStrings == null ? files : files
                .Where(f => sourcePathExcludeRegexStrings.All(r => !r.IsMatch(f.SourcePath)));
        }
    }
}