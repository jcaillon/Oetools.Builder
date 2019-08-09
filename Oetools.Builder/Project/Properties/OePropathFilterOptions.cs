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
using DotUtilities.Attributes;
using Oetools.Builder.Project.Task;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Project.Properties {

    /// <inheritdoc cref="OeProperties.PropathSourceDirectoriesFilter"/>
    [Serializable]
    public class OePropathFilterOptions : PathListerFilterOptions {

        /// <summary>
        /// A path pattern that describes paths that should be included by this filter.
        /// </summary>
        /// <inheritdoc cref="AOeTaskFilter.Include"/>
        [XmlElement(ElementName = "PropathInclude")]
        public override string Include {
            get => base.Include;
            set => base.Include = value;
        }

        /// <summary>
        /// A regular expression that describes paths that should be included by this filter.
        /// </summary>
        /// <inheritdoc cref="AOeTaskFilter.IncludeRegex"/>
        [XmlElement(ElementName = "PropathIncludeRegex")]
        public override string IncludeRegex {
            get => base.IncludeRegex;
            set => base.IncludeRegex = value;
        }

        /// <summary>
        /// A path pattern that describes paths that should be excluded by this filter.
        /// </summary>
        /// <inheritdoc cref="AOeTaskFilter.Exclude"/>
        [XmlElement(ElementName = "PropathExclude")]
        public override string Exclude {
            get => base.Exclude;
            set => base.Exclude = value;
        }

        /// <summary>
        /// A regular expression that describes paths that should be excluded by this filter.
        /// </summary>
        /// <inheritdoc cref="AOeTaskFilter.ExcludeRegex"/>
        [XmlElement(ElementName = "PropathExcludeRegex")]
        public override string ExcludeRegex {
            get => base.ExcludeRegex;
            set => base.ExcludeRegex = value;
        }

        /// <inheritdoc cref="PathListerFilterOptions.ExcludeHiddenDirectories"/>
        [XmlElement(ElementName = "PropathExcludeHiddenDirectories")]
        [DefaultValueMethod(nameof(GetDefaultExcludeHiddenDirectories))]
        public override bool? ExcludeHiddenDirectories {
            get => base.ExcludeHiddenDirectories;
            set => base.ExcludeHiddenDirectories = value;
        }

        /// <inheritdoc cref="PathListerFilterOptions.RecursiveListing"/>
        [XmlElement(ElementName = "PropathRecursiveListing")]
        [DefaultValueMethod(nameof(GetDefaultRecursiveListing))]
        public override bool? RecursiveListing {
            get => base.RecursiveListing;
            set => base.RecursiveListing = value;
        }

        /// <inheritdoc cref="PathListerFilterOptions.ExtraVcsPatternExclusion"/>
        [XmlElement(ElementName = "PropathExtraVcsPatternExclusion")]
        [DefaultValueMethod(nameof(GetDefaultExtraVcsPatternExclusion))]
        public override string ExtraVcsPatternExclusion {
            get => base.ExtraVcsPatternExclusion;
            set => base.ExtraVcsPatternExclusion = value;
        }
    }
}
