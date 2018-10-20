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

using System.Xml.Serialization;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project {
    public class OeFilterOptions : OeTaskFilter {
        
        /// <summary>
        /// Whether or not to ignore hidden directories during the listing.
        /// </summary>
        [XmlElement(ElementName = "ExcludeHiddenDirectories")]
        public bool? ExcludeHiddenDirectories { get; set; }
        public static bool GetDefaultExcludeHiddenDirectories() => false;
        
        /// <summary>
        /// Whether or not to include the content of subdirectories when listing.
        /// </summary>
        [XmlElement(ElementName = "RecursiveListing")]
        public bool? RecursiveListing { get; set; } = true;
        public static bool GetDefaultRecursiveListing() => true;

        /// <summary>
        /// Extra patterns of path to exclude during a listing, corresponds to typical svn/git directories that we don't want to include in builds.
        /// </summary>
        [XmlElement(ElementName = "ExtraVcsPatternExclusion")]
        public string ExtraVcsPatternExclusion { get; set; }
        public static string GetDefaultExtraVcsPatternExclusion() => OeBuilderConstants.VcsDirectoryExclusions;

       
        /// <inheritdoc cref="OeTask.ExecuteInternal"/>
        protected override void ExecuteInternal() {
            // does nothing
        }
        
        /// <inheritdoc cref="OeTask.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // does nothing
        }
    }
}