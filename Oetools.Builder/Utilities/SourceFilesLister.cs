#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (SourceFilesLister.cs) is part of Oetools.Builder.
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
using System.Text.RegularExpressions;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Utilities {
    
    public class SourceFilesLister {
        
        public ILogger Log { protected get; set; }
        
        public string SourceDirectory { get; }

        public IOeTaskFilter SourcePathFilter { get; set; }

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

        public FileList<OeFile> PreviousSourceFiles { get; set; }
        
        public CancellationTokenSource CancelSource { get; set; }
        
        private List<Regex> _defaultFilters;

        private static bool ExcludeHiddenFolders => false;
        
        public bool SetFileInfoAndState { get; set; }

        public SourceFilesLister(string sourceDirectory, CancellationTokenSource cancelSource = null) {
            SourceDirectory = sourceDirectory.ToCleanPath();
            CancelSource = cancelSource;
        }

        /// <summary>
        /// Returns a list of existing files in the <see cref="SourceDirectory"/>, considering all the filter options in this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public FileList<OeFile> GetFileList() {
            IEnumerable<string> listedFiles;
            if (SourcePathGitFilter != null && ((SourcePathGitFilter.OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch ?? OeGitFilter.GetDefaultOnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch()) || (SourcePathGitFilter.OnlyIncludeSourceFilesModifiedSinceLastCommit ?? OeGitFilter.GetDefaultOnlyIncludeSourceFilesModifiedSinceLastCommit()))) {
                listedFiles = GetBaseFileListFromGit();
            } else {
                listedFiles = GetBaseFileList();
            }

            var output = new FileList<OeFile>();
            foreach (var file in listedFiles) {
                CancelSource?.Token.ThrowIfCancellationRequested();
                if (output.Contains(file)) {
                    continue;
                }
                
                var oeFile = new OeFile(file); // all the files here exist
                output.Add(oeFile);
                
                if (SetFileInfoAndState) {
                    // get info on each files
                    SetFileBaseInfo(oeFile);
                    // get the state of each file (added/unchanged/modified)
                    SetFileState(oeFile);
                }
            }
            return output;
        }

        /// <summary>
        /// Returns a list of directories in the <see cref="SourceDirectory"/>, considering all the filter options in this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<string> GetDirectoryList() {
            var sourcePathExcludeRegexStrings = GetExclusionRegexStringsFromFilters(SourcePathFilter);
            return Utils.EnumerateAllFolders(SourceDirectory, SearchOption.AllDirectories, sourcePathExcludeRegexStrings, ExcludeHiddenFolders, CancelSource)
                .Where(IsFileIncludedBySourcePathFilters);
        }

        /// <summary>
        /// Returns a list of all the files in the source directory, filtered with <see cref="SourcePathFilter"/>
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetBaseFileList() {
            var sourcePathExcludeRegexStrings = GetExclusionRegexStringsFromFilters(SourcePathFilter);
            return Utils.EnumerateAllFiles(SourceDirectory, SearchOption.AllDirectories, sourcePathExcludeRegexStrings, ExcludeHiddenFolders, CancelSource)
                .Where(IsFileIncludedBySourcePathFilters);
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
            var previousFile = PreviousSourceFiles[oeFile];
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
                oeFile.Hash = Utils.GetMd5FromFilePath(oeFile.SourceFilePath);
            } catch (Exception e) {
                throw new Exception($"Error getting information on file {oeFile.SourceFilePath}, check permissions", e);
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
                var fileInfo = new FileInfo(oeFile.SourceFilePath);
                oeFile.Size = fileInfo.Length;
                oeFile.LastWriteTime = fileInfo.LastWriteTime;
            } catch (Exception e) {
                throw new Exception($"Error getting information on file {oeFile.SourceFilePath}, check permissions", e);
            }
        }

        /// <summary>
        /// Returns a list of files in the source directory based on a git filter and also filtered with <see cref="SourcePathFilter"/>
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetBaseFileListFromGit() {
            var output = new List<string>();

            var gitManager = new GitManager {
                Log = Log
            };
            gitManager.SetCurrentDirectory(SourceDirectory);
            
            if (SourcePathGitFilter.OnlyIncludeSourceFilesModifiedSinceLastCommit ?? OeGitFilter.GetDefaultOnlyIncludeSourceFilesModifiedSinceLastCommit()) {
                output.AddRange(gitManager.GetAllModifiedFilesSinceLastCommit());
            }
            
            if (SourcePathGitFilter.OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch ?? OeGitFilter.GetDefaultOnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch()) {
                try {
                    output.AddRange(gitManager.GetAllCommittedFilesExclusiveToCurrentBranch(SourcePathGitFilter.CurrentBranchOriginCommit, SourcePathGitFilter.CurrentBranchName));
                } catch (GitManagerCantFindMergeCommitException) {
                    // this exception means we can't find commits that exist only on that branch
                    // we list every file committed in the repo instead (= all files in repo minus all modified files)
                    var allFiles = GetBaseFileList();
                    var workingFiles = gitManager.GetAllModifiedFilesSinceLastCommit().ToHashSet(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    output.AddRange(allFiles.Select(f => f.FromAbsolutePathToRelativePath(SourceDirectory)).Where(f => !workingFiles.Contains(f)));
                }
            }
            
            // Git returns relative path, convert them into absolute path, 
            // it can also return deleted files, keep only existing files
            // finally, apply the exclusion filter
            return output
                .Select(s => Path.Combine(SourceDirectory, s.ToCleanPath()))
                .Where(File.Exists)
                .Where(IsFilePassingSourcePathFilters);
        }
        
        /// <summary>
        /// Filter the list of files with the <see cref="SourcePathFilter"/>
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public IEnumerable<OeFile> FilterSourceFiles(IEnumerable<OeFile> files) {
            return files.Where(f => IsFilePassingDefaultFilters(f.SourceFilePath) && IsFilePassingSourcePathFilters(f.SourceFilePath));
        }
        
        /// <summary>
        /// Returns true if the given is excluded with the current filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsFilePassingDefaultFilters(string filePath) {
            return GetDefaultFilters().All(regex => !regex.IsMatch(filePath));
        }
        
        /// <summary>
        /// Returns true if the given is excluded with the current filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsFilePassingSourcePathFilters(string filePath) {
            return SourcePathFilter == null || SourcePathFilter.IsFilePassingFilter(filePath);
        }
        
        private bool IsFileIncludedBySourcePathFilters(string filePath) {
            return SourcePathFilter == null || SourcePathFilter.IsFileIncluded(filePath);
        }
        
        private List<Regex> GetDefaultFilters() {
            if (_defaultFilters == null) {
                _defaultFilters = GetDefaultFiltersRegexes().Select(s => new Regex(s)).ToList();
            }
            return _defaultFilters;
        }

        /// <summary>
        /// Get a list of regexes corresponding to default filters (.git/.svn folders)
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetDefaultFiltersRegexes() {
            return OeBuilderConstants.ExtraSourceDirectoryExclusions.Split(';').Select(s => Path.Combine(SourceDirectory, s).PathWildCardToRegex());
        }
        
        /// <summary>
        /// Get a list of exclusion regex strings from a list of filters, appending extra exclusions
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private List<string> GetExclusionRegexStringsFromFilters(IOeTaskFilter filter) {
            var exclusionRegexStrings = GetDefaultFiltersRegexes().ToList();
            if (filter != null) {
                exclusionRegexStrings.AddRange(filter.GetRegexExcludeStrings());
            }
            return exclusionRegexStrings;
        }
    }
}