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
    /// <summary>
    /// Allows to list either files or directories within a base directory. Has tons of options for listing.
    /// </summary>
    public class PathLister {
        
        /// <summary>
        /// Logger instance.
        /// </summary>
        public ILogger Log { private get; set; }

        /// <summary>
        /// A path filter that has include/exclude patterns to filter paths.
        /// </summary>
        public IOeTaskFilter PathFilter { get; set; }

        /// <summary>
        /// A git based filter.
        /// </summary>
        public OeGitFilterBuildOptions GitFilter { get; set; }
        
        /// <summary>
        /// <para>
        /// If non null, the lister will also set info on each file listed :
        /// - <see cref="OeFile.LastWriteTime"/>.
        /// - <see cref="OeFile.Size"/>.
        /// - <see cref="OeFile.State"/>.
        /// </para>
        /// </summary>
        public PathListerFileInfoOptions FileInfoOptions { get; set; }
        
        /// <summary>
        /// Whether or not to ignore hidden directories during the listing.
        /// </summary>
        public bool ExcludeHiddenDirectories { get; set; } = false;
        
        /// <summary>
        /// Whether or not to browse sub directories when listing.
        /// </summary>
        public bool RecursiveListing { get; set; } = true;

        /// <summary>
        /// The default pattern of path to exclude, corresponds to typical svn/git folders.
        /// </summary>
        public string DefaultVcsPatternExclusion { get; set; } = OeBuilderConstants.VcsDirectoryExclusions;

        /// <summary>
        /// The base source directory to list.
        /// </summary>
        private string BaseDirectory { get; }
        
        private CancellationToken? CancelToken { get; }
        
        private List<Regex> _defaultFilters;
        
        /// <summary>
        /// New instance.
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="cancelToken"></param>
        public PathLister(string sourceDirectory, CancellationToken? cancelToken = null) {
            BaseDirectory = sourceDirectory.ToCleanPath();
            CancelToken = cancelToken;
        }

        /// <summary>
        /// Returns a list of existing and unique files in the <see cref="BaseDirectory"/>, considering all the filter options.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public PathList<OeFile> GetFileList() {
            IEnumerable<string> listedFiles;
            if (GitFilter != null && GitFilter.IsActive()) {
                listedFiles = GetBaseFileListFromGit();
            } else {
                listedFiles = GetBaseFileList();
            }

            var output = new PathList<OeFile>();
            foreach (var file in listedFiles) {
                CancelToken?.ThrowIfCancellationRequested();
                
                if (output.Contains(file)) {
                    continue;
                }
                
                var oeFile = new OeFile(file); // all the files here exist
                output.Add(oeFile);
                
                if (FileInfoOptions != null) {
                    // get info on each files
                    SetFileBaseInfo(oeFile);
                    // get the state of each file (added/unchanged/modified)
                    SetFileState(oeFile);
                }
            }
            return output;
        }

        /// <summary>
        /// Returns a list of existing and unique directories in the <see cref="BaseDirectory"/>, considering all the filter options.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public PathList<OeDirectory> GetDirectoryList() {
            var sourcePathExcludeRegexStrings = GetExclusionRegexStringsFromFilters(PathFilter);
            return Utils.EnumerateAllFolders(BaseDirectory, RecursiveListing ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly, sourcePathExcludeRegexStrings, ExcludeHiddenDirectories, CancelToken)
                .Where(IsFileIncludedBySourcePathFilters)
                .Select(d => new OeDirectory(d))
                .ToFileList();
        }

        /// <summary>
        /// Returns a list of all the files in the source directory, filtered with <see cref="PathFilter"/>
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetBaseFileList() {
            var sourcePathExcludeRegexStrings = GetExclusionRegexStringsFromFilters(PathFilter);
            return Utils.EnumerateAllFiles(BaseDirectory, RecursiveListing ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly, sourcePathExcludeRegexStrings, ExcludeHiddenDirectories, CancelToken)
                .Where(IsFileIncludedBySourcePathFilters);
        }

        /// <summary>
        /// Sets the <see cref="OeFile.State"/> of a file
        /// </summary>
        /// <param name="oeFile"></param>
        /// <exception cref="Exception"></exception>
        private void SetFileState(OeFile oeFile) {
            var previousFile = FileInfoOptions?.GetPreviousFileImage?.Invoke(oeFile.Path);
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
        /// Has the file changed since <paramref name="previous" /> version to <paramref name="now" /> version
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool HasFileChanged(OeFile previous, OeFile now) {
            // test if it has changed
            if (previous.Size.Equals(now.Size)) {
                if (!(FileInfoOptions?.UseLastWriteDateComparison ?? false) || previous.LastWriteTime.Equals(now.LastWriteTime)) {
                    if (!(FileInfoOptions?.UseHashComparison ?? false) || !string.IsNullOrEmpty(previous.Hash) && previous.Hash.Equals(SetFileHash(now).Hash, StringComparison.OrdinalIgnoreCase)) {
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
                oeFile.Hash = Utils.GetMd5FromFilePath(oeFile.Path);
            } catch (Exception e) {
                throw new Exception($"Error getting information on file {oeFile.Path}, check permissions", e);
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
                var fileInfo = new FileInfo(oeFile.Path);
                oeFile.Size = fileInfo.Length;
                oeFile.LastWriteTime = fileInfo.LastWriteTime;
            } catch (Exception e) {
                throw new Exception($"Error getting information on file {oeFile.Path}, check permissions", e);
            }
        }

        /// <summary>
        /// Returns a list of files in the source directory based on a git filter and also filtered with <see cref="PathFilter"/>
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetBaseFileListFromGit() {
            var output = new List<string>();

            var gitManager = new GitManager {
                Log = Log
            };
            gitManager.SetCurrentDirectory(BaseDirectory);
            
            if (GitFilter.OnlyIncludeSourceFilesModifiedSinceLastCommit ?? OeGitFilterBuildOptions.GetDefaultOnlyIncludeSourceFilesModifiedSinceLastCommit()) {
                output.AddRange(gitManager.GetAllModifiedFilesSinceLastCommit());
            }
            
            if (GitFilter.OnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch ?? OeGitFilterBuildOptions.GetDefaultOnlyIncludeSourceFilesCommittedOnlyOnCurrentBranch()) {
                try {
                    output.AddRange(gitManager.GetAllCommittedFilesExclusiveToCurrentBranch(GitFilter.CurrentBranchOriginCommit, GitFilter.CurrentBranchName));
                } catch (GitManagerCantFindMergeCommitException) {
                    // this exception means we can't find commits that exist only on that branch
                    // we list every file committed in the repo instead (= all files in repo minus all modified files)
                    var allFiles = GetBaseFileList();
                    var workingFiles = gitManager.GetAllModifiedFilesSinceLastCommit().ToHashSet(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    output.AddRange(allFiles.Select(f => f.FromAbsolutePathToRelativePath(BaseDirectory)).Where(f => !workingFiles.Contains(f)));
                }
            }
            
            // Git returns relative path, convert them into absolute path, 
            // it can also return deleted files, keep only existing files
            // finally, apply the exclusion filter
            return output
                .Select(s => Path.Combine(BaseDirectory, s.ToCleanPath()))
                .Where(File.Exists)
                .Where(IsFilePassingSourcePathFilters);
        }
        
        /// <summary>
        /// Filter the list of files with the <see cref="PathFilter"/>
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public IEnumerable<OeFile> FilterSourceFiles(IEnumerable<OeFile> files) {
            return files.Where(f => IsFilePassingDefaultFilters(f.Path) && IsFilePassingSourcePathFilters(f.Path));
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
            return PathFilter == null || PathFilter.IsFilePassingFilter(filePath);
        }
        
        private bool IsFileIncludedBySourcePathFilters(string filePath) {
            return PathFilter == null || PathFilter.IsFileIncluded(filePath);
        }
        
        private List<Regex> GetDefaultFilters() {
            if (_defaultFilters == null) {
                _defaultFilters = GetDefaultVcsFiltersRegexes().Select(s => new Regex(s)).ToList();
            }
            return _defaultFilters;
        }

        /// <summary>
        /// Get a list of regexes corresponding to default filters (.git/.svn folders)
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetDefaultVcsFiltersRegexes() {
            return DefaultVcsPatternExclusion?.Split(';').Select(s => Path.Combine(BaseDirectory, s).PathWildCardToRegex()) ?? Enumerable.Empty<string>();
        }
        
        /// <summary>
        /// Get a list of exclusion regex strings from a list of filters, appending extra exclusions
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private List<string> GetExclusionRegexStringsFromFilters(IOeTaskFilter filter) {
            var exclusionRegexStrings = GetDefaultVcsFiltersRegexes().ToList();
            if (filter != null) {
                exclusionRegexStrings.AddRange(filter.GetRegexExcludeStrings());
            }
            return exclusionRegexStrings;
        }
    }
}