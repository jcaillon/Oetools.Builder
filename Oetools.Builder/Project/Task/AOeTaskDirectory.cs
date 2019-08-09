#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFile.cs) is part of Oetools.Builder.
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

using System.IO;
using System.Xml.Serialization;
using DotUtilities;
using DotUtilities.Extensions;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Project.Properties;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// A task that operates on directories.
    /// </summary>
    /// <inheritdoc cref="AOeTaskFilter"/>
    public abstract class AOeTaskDirectory : AOeTaskFilterAttributes, IOeTaskDirectory {

        /// <summary>
        /// Not applicable.
        /// </summary>
        [XmlIgnore]
        public override string IncludeRegex => null;

        private PathList<IOeDirectory> _directoriesToProcess;
        private bool _areDirectoriesToProcessInitialized;

        /// <inheritdoc cref="IOeTaskDirectory.ValidateCanGetDirectoriesToProcessFromIncludes"/>
        public void ValidateCanGetDirectoriesToProcessFromIncludes() {
            if (string.IsNullOrEmpty(Include)) {
                throw new TaskValidationException(this, $"This task needs to have the property {GetType().GetXmlName(nameof(Include))} defined or it can not be applied on any file.");
            }
            if (!string.IsNullOrEmpty(IncludeRegex)) {
                throw new TaskValidationException(this, $"The property {GetType().GetXmlName(nameof(IncludeRegex))} is not allowed in this task for that build step because it would not allow to find files to include (it would require to list the entire content of all the discs on this computer to match this regular expression), use the property {GetType().GetXmlName(nameof(Include))} exclusively.");
            }
        }

        /// <inheritdoc cref="IOeTaskDirectory.SetDirectoriesToProcess"/>
        public void SetDirectoriesToProcess(PathList<IOeDirectory> pathsToBuild) {
            _directoriesToProcess = pathsToBuild;
            _areDirectoriesToProcessInitialized = true;
        }

        /// <inheritdoc cref="IOeTaskDirectory.GetDirectoriesToProcess"/>
        public PathList<IOeDirectory> GetDirectoriesToProcess() => _directoriesToProcess;

        /// <inheritdoc cref="IOeTaskFile.GetFilesToProcessFromIncludes"/>
        public PathList<IOeDirectory> GetDirectoriesToProcessFromIncludes() {
            var output = new PathList<IOeDirectory>();
            var i = 0;
            foreach (var path in GetIncludeStrings()) {
                if (Directory.Exists(path)) {
                    // the include directly designate a dir
                    if (!IsPathExcluded(path)) {
                        output.TryAdd(new OeDirectory(Path.GetFullPath(path).ToCleanPath()));
                    }
                    continue;
                }

                // the include is a wildcard path, we try to get the "root" folder to list to get all the dir
                var validDir = Utils.GetLongestValidDirectory(path);
                var useCurrentDirectory = string.IsNullOrEmpty(validDir);
                validDir = validDir ?? Directory.GetCurrentDirectory();

                var filter = new OeSourceFilterOptions {
                    Include = useCurrentDirectory ? Path.Combine(validDir, path) : path,
                    Exclude = Exclude,
                    ExcludeRegex = ExcludeRegex,
                    RecursiveListing = path.Contains("**") || path.LastIndexOf('/') > path.LastIndexOf('*') || path.LastIndexOf('\\') > path.LastIndexOf('*')
                };

                Log?.Info($"Listing directories from: {validDir.PrettyQuote()}.");
                Log?.Debug($"Wildcards path: {filter.Include.PrettyQuote()}.");

                var regexCorrespondingToPath = GetIncludeRegex()[i];
                foreach (var file in new PathLister(validDir, CancelToken) { FilterOptions = filter, Log = Log } .GetDirectoryList()) {
                    if (!useCurrentDirectory && regexCorrespondingToPath.IsMatch(file.Path) ||
                        useCurrentDirectory && regexCorrespondingToPath.IsMatch(file.Path.ToRelativePath(validDir))) {
                        output.TryAdd(file);
                    }
                }

                i++;
            }
            return output;
        }

        /// <inheritdoc />
        protected override void PreExecuteInternal() {
            base.PreExecuteInternal();
            if (!_areDirectoriesToProcessInitialized) {
                SetDirectoriesToProcess(GetDirectoriesToProcessFromIncludes());
            }
        }

    }
}
