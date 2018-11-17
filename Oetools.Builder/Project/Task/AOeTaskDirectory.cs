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
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {

    /// <summary>
    /// A task that operates on directories.
    /// </summary>
    /// <inheritdoc cref="AOeTaskFilter"/>
    public abstract class AOeTaskDirectory : AOeTaskFilter, IOeTaskDirectory {

        /// <summary>
        /// Not applicable.
        /// </summary>
        [XmlIgnore]
        public override string IncludeRegex => null;
        
        private PathList<IOeDirectory> _directoriesToBuild;

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
        public void SetDirectoriesToProcess(PathList<IOeDirectory> pathsToBuild) => _directoriesToBuild = pathsToBuild;

        /// <inheritdoc cref="IOeTaskDirectory.GetDirectoriesToProcess"/>
        public PathList<IOeDirectory> GetDirectoriesToProcess() => _directoriesToBuild;

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
                } else {
                    // the include is a wildcard path, we try to get the "root" folder to list to get all the dir
                    var validDir = Utils.GetLongestValidDirectory(path);
                    if (!string.IsNullOrEmpty(validDir)) {
                        Log?.Info($"Listing directory : {validDir.PrettyQuote()}");
                        var regexCorrespondingToPath = GetIncludeRegex()[i];
                        foreach (var directory in new PathLister(validDir, CancelToken) { Filter = this, Log = Log } .GetDirectoryList()) {
                            if (regexCorrespondingToPath.IsMatch(directory.Path)) {
                                output.TryAdd(directory);
                            }
                        }
                    } else {
                        AddExecutionWarning(new TaskExecutionException(this, $"The property {GetType().GetXmlName(nameof(Include))} part {i} does not designate a file (e.g. /dir/file.ext) nor does it allow to find a base directory to list (e.g. /dir/**), the path in error is : {path}."));
                    }
                }
                i++;
            }
            return output;
        }
        
        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected sealed override void ExecuteInternal() {
            ExecuteForDirectoriesInternal(_directoriesToBuild);
        }

        /// <summary>
        /// Execute the task for a set of directories.
        /// </summary>
        /// <remarks>
        /// <para>
        /// - This method should throw <see cref="TaskExecutionException"/> if needed
        /// - This method can publish warnings using <see cref="AOeTask.AddExecutionWarning"/>
        /// </para>
        /// </remarks>
        /// <param name="directories"></param>
        /// <exception cref="TaskExecutionException"></exception>
        protected abstract void ExecuteForDirectoriesInternal(PathList<IOeDirectory> directories);

    }
}