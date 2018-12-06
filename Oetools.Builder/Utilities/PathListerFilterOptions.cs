#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFilterOptions.cs) is part of Oetools.Builder.
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
using System.Xml.Serialization;
using Oetools.Builder.Project.Task;

namespace Oetools.Builder.Utilities {
    
    /// <summary>
    /// Path filtering options.
    /// </summary>
    [Serializable]
    public abstract class PathListerFilterOptions : AOeTaskFilter {
        
        /// <summary>
        /// Whether or not to ignore hidden directories during the listing.
        /// </summary>
        [XmlIgnore]
        public virtual bool? ExcludeHiddenDirectories { get; set; }
        public static bool GetDefaultExcludeHiddenDirectories() => false;
        
        /// <summary>
        /// Whether or not to include the content of subdirectories when listing.
        /// </summary>
        [XmlIgnore]
        public virtual bool? RecursiveListing { get; set; }
        public static bool GetDefaultRecursiveListing() => true;

        /// <summary>
        /// Extra patterns of path to exclude during a listing, corresponds to typical svn/git directories that we don't want to include in a build.
        /// </summary>
        /// <remarks>
        /// The pattern are relative to the source directory.
        /// </remarks>
        [XmlIgnore]
        public virtual string ExtraVcsPatternExclusion { get; set; }
        public static string GetDefaultExtraVcsPatternExclusion() => OeBuilderConstants.VcsDirectoryExclusions;

        /// <summary>
        /// The paths to use as results for this filter.
        /// </summary>
        /// <remarks>
        /// Several paths can be given by separating them with a semi-colon (i.e. ;).
        /// This option overrides every other options in this filter, the paths listed here are directly used as results of this filter.
        /// Non existing paths are simply ignored.
        /// </remarks>
        [XmlIgnore]
        public virtual string OverrideOutputList { get; set; }
       
        /// <inheritdoc cref="AOeTask.ExecuteInternal"/>
        protected override void ExecuteInternal() {
            // does nothing
        }
        
        /// <inheritdoc cref="AOeTask.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // does nothing
        }
    }
}