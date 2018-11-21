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
using Oetools.Builder.Project.Properties;
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
        /// Full filtering options for this listing.
        /// </summary>
        internal IOeTaskFilter Filter {
            get => _filterOptions ?? _filter;
            set {
                if (_filterOptions != null) {
                    throw new Exception($"{nameof(FilterOptions)} already defined.");
                }
                _filter = value;
            }
        }

        /// <summary>
        /// Full filtering options for this listing.
        /// </summary>
        public PathListerFilterOptions FilterOptions {
            get => _filterOptions;
            set {
                if (_filter != null) {
                    throw new Exception($"{nameof(Filter)} already defined.");
                }
                _filterOptions = value;
            }
        }

        /// <summary>
        /// A git based filter.
        /// </summary>
        public PathListerGitFilterOptions GitFilter { get; set; }
        
        /// <summary>
        /// <para>
        /// If non null, the lister will also set info on each file listed :
        /// - <see cref="OeFile.LastWriteTime"/>.
        /// - <see cref="OeFile.Size"/>.
        /// - <see cref="OeFile.State"/>.
        /// </para>
        /// </summary>
        public PathListerOutputOptions OutputOptions { get; set; }
        
        /// <summary>
        /// The base source directory to list.
        /// </summary>
        private string BaseDirectory { get; }
        
        private CancellationToken? CancelToken { get; }
        
        private List<Regex> _defaultFilters;
        private PathListerFilterOptions _filterOptions;
        private IOeTaskFilter _filter;

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
        public PathList<IOeFile> GetFileList() {
            PathList<IOeFile> listedFiles;
            
            if (!string.IsNullOrEmpty(FilterOptions?.OverrideOutputList)) {
                Log?.Trace?.Write("Using pre-calculated list of files.");
                
                listedFiles = new PathList<IOeFile>();
                foreach (var path in FilterOptions?.OverrideOutputList.Split(';')) {
                    if (File.Exists(path)) {
                        if (!listedFiles.TryAdd(new OeFile(path))) {
                            Log?.Trace?.Write($"Duplicated path: {path}.");
                        }
                    }
                }
            } else {
                if (GitFilter != null && GitFilter.IsActive()) {
                    listedFiles = GetBaseFileListFromGit();
                } else {
                    listedFiles = GetBaseFileList();
                }
            }

            if (OutputOptions != null) {
                foreach (var file in listedFiles) {
                    CancelToken?.ThrowIfCancellationRequested();

                    // get info on each files
                    SetFileBaseInfo(file);
                    // get the state of each file (added/unchanged/modified)
                    SetFileState(file);

                }
            }
            return listedFiles;
        }

        /// <summary>
        /// Returns a list of existing and unique directories in the <see cref="BaseDirectory"/>, considering all the filter options.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public PathList<IOeDirectory> GetDirectoryList() {
            if (!string.IsNullOrEmpty(FilterOptions?.OverrideOutputList)) {
                Log?.Trace?.Write("Using pre-calculated list of directories.");
                
                var listedDirectories = new PathList<IOeDirectory>();
                foreach (var path in FilterOptions?.OverrideOutputList.Split(';')) {
                    if (Directory.Exists(path)) {
                        if (!listedDirectories.TryAdd(new OeDirectory(path))) {
                            Log?.Trace?.Write($"Duplicated path: {path}.");
                        }
                    }
                }
                return listedDirectories;
            }
            
            var sourcePathExcludeRegexStrings = GetExclusionRegexStringsFromFilters(Filter);
            return Utils.EnumerateAllFolders(BaseDirectory, FilterOptions?.RecursiveListing ?? PathListerFilterOptions.GetDefaultRecursiveListing() ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly, sourcePathExcludeRegexStrings, FilterOptions?.ExcludeHiddenDirectories ?? PathListerFilterOptions.GetDefaultExcludeHiddenDirectories(), CancelToken)
                .Where(IsFileIncludedBySourcePathFilters)
                .Select(d => new OeDirectory(d) as IOeDirectory)
                .ToFileList();
        }

        /// <summary>
        /// Returns a list of all the files in the source directory, filtered with <see cref="FilterOptions"/>
        /// </summary>
        /// <returns></returns>
        private PathList<IOeFile> GetBaseFileList() {
            var sourcePathExcludeRegexStrings = GetExclusionRegexStringsFromFilters(Filter);
            return Utils.EnumerateAllFiles(BaseDirectory, FilterOptions?.RecursiveListing ?? PathListerFilterOptions.GetDefaultRecursiveListing() ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly, sourcePathExcludeRegexStrings, FilterOptions?.ExcludeHiddenDirectories ?? PathListerFilterOptions.GetDefaultExcludeHiddenDirectories(), CancelToken)
                .Where(IsFileIncludedBySourcePathFilters)
                .Select(f => new OeFile(f) as IOeFile)
                .ToFileList();
        }

        /// <summary>
        /// Returns a list of files in the source directory based on a git filter and also filtered with <see cref="FilterOptions"/>
        /// </summary>
        /// <returns></returns>
        private PathList<IOeFile> GetBaseFileListFromGit() {
            var output = new PathList<IOeFile>();

            var gitManager = new GitManager {
                Log = Log
            };
            gitManager.SetCurrentDirectory(BaseDirectory);
            
            if (GitFilter.IncludeSourceFilesModifiedSinceLastCommit ?? OeGitFilterOptions.GetDefaultIncludeSourceFilesModifiedSinceLastCommit()) {
                output.AddRange(gitManager.GetAllModifiedFilesSinceLastCommit()
                    .Select(f => new OeFile(f.MakePathAbsolute(BaseDirectory).ToCleanPath()))
                    .ToFileList());
            }
            
            if (GitFilter.IncludeSourceFilesCommittedOnlyOnCurrentBranch ?? OeGitFilterOptions.GetDefaultIncludeSourceFilesCommittedOnlyOnCurrentBranch()) {
                try {
                    output.AddRange(gitManager.GetAllCommittedFilesExclusiveToCurrentBranch(GitFilter.CurrentBranchOriginCommit, GitFilter.CurrentBranchName)
                        .Select(f => new OeFile(f.MakePathAbsolute(BaseDirectory).ToCleanPath()))
                        .ToFileList());
                } catch (GitManagerCantFindMergeCommitException) {
                    // this exception means we can't find commits that exist only on that branch
                    // we list every file committed in the repo instead (= all files in repo minus all modified files)
                    var allFiles = GetBaseFileList();
                    var workingFiles = gitManager.GetAllModifiedFilesSinceLastCommit()
                        .Select(f => f.MakePathAbsolute(BaseDirectory).ToCleanPath())
                        .ToHashSet(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    output.AddRange(allFiles.Where(f => !workingFiles.Contains(f.Path)));
                }
            }
            
            // TODO : also apply RecursiveListing / ExcludeHiddenDirectories options
            
            // Git can return deleted files, keep only existing files
            // finally, apply the inclusion/exclusion filters
            return output.CopyWhere(f => File.Exists(f.Path) && IsFilePassingDefaultFilters(f.Path) && IsFilePassingSourcePathFilters(f.Path));
        }
        
        /// <summary>
        /// Sets the <see cref="OeFile.State"/> of a file
        /// </summary>
        /// <param name="oeFile"></param>
        /// <exception cref="Exception"></exception>
        private void SetFileState(IOeFile oeFile) {
            var previousFile = OutputOptions?.GetPreviousFileImage?.Invoke(oeFile.Path);
            if (previousFile == null || previousFile.State == OeFileState.Deleted) {
                oeFile.State = OeFileState.Added;
            } else {
                if (HasFileChanged(previousFile, oeFile)) {
                    oeFile.State = OeFileState.Modified;
                } else {
                    oeFile.State = OeFileState.Unchanged;
                    oeFile.Checksum = previousFile.Checksum ?? oeFile.Checksum;
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
        private bool HasFileChanged(IOeFile previous, IOeFile now) {
            // test if it has changed
            if (previous.Size.Equals(now.Size)) {
                if (!(OutputOptions?.UseLastWriteDateComparison ?? false) || previous.LastWriteTime.Equals(now.LastWriteTime)) {
                    if (!(OutputOptions?.UseCheckSumComparison ?? false) || !string.IsNullOrEmpty(previous.Checksum) && previous.Checksum.Equals(SetFileHash(now).Checksum, StringComparison.OrdinalIgnoreCase)) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Sets the <see cref="OeFile.Checksum"/> of a file if needed
        /// </summary>
        /// <param name="oeFile"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IOeFile SetFileHash(IOeFile oeFile) {
            if (!string.IsNullOrEmpty(oeFile.Checksum)) {
                return oeFile;
            }
            try {
                oeFile.Checksum = Utils.GetMd5FromFilePath(oeFile.Path);
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
        private static void SetFileBaseInfo(IOeFile oeFile) {
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
        /// Filter the list of files with the <see cref="FilterOptions"/>
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public IEnumerable<IOeFile> FilterSourceFiles(IEnumerable<IOeFile> files) {
            return files.Where(f => IsFilePassingDefaultFilters(f.Path) && IsFilePassingSourcePathFilters(f.Path));
        }
        
        /// <summary>
        /// Returns true if the given is excluded with the current filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool IsFilePassingDefaultFilters(string filePath) {
            return GetDefaultFilters().All(regex => !regex.IsMatch(filePath));
        }
        
        /// <summary>
        /// Returns true if the given is excluded with the current filter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool IsFilePassingSourcePathFilters(string filePath) {
            return Filter == null || Filter.IsPathPassingFilter(filePath);
        }
        
        private bool IsFileIncludedBySourcePathFilters(string filePath) {
            return Filter == null || Filter.IsPathIncluded(filePath);
        }
        
        private List<Regex> GetDefaultFilters() {
            if (_defaultFilters == null) {
                _defaultFilters = GetExtraVcsFiltersRegexes().Select(s => new Regex(s)).ToList();
            }
            return _defaultFilters;
        }

        /// <summary>
        /// Get a list of regexes corresponding to default filters (.git/.svn folders).
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetExtraVcsFiltersRegexes() {
            if (FilterOptions?.ExtraVcsPatternExclusion != null && string.IsNullOrWhiteSpace(FilterOptions.ExtraVcsPatternExclusion)) {
                return Enumerable.Empty<string>();
            }
            return (FilterOptions?.ExtraVcsPatternExclusion ?? PathListerFilterOptions.GetDefaultExtraVcsPatternExclusion())?.Split(';').Select(s => Path.Combine(BaseDirectory, s).PathWildCardToRegex());
        }
        
        /// <summary>
        /// Get a list of exclusion regex strings from a list of filters, appending extra exclusions
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private List<string> GetExclusionRegexStringsFromFilters(IOeTaskFilter filter) {
            var exclusionRegexStrings = GetExtraVcsFiltersRegexes().ToList();
            if (filter != null) {
                exclusionRegexStrings.AddRange(filter.GetRegexExcludeStrings());
            }
            return exclusionRegexStrings;
        }
    }
}