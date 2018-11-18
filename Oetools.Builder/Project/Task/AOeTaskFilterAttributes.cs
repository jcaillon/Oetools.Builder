#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (AOeTaskFilterAttributes.cs) is part of Oetools.Builder.
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

namespace Oetools.Builder.Project.Task {
    
    /// <inheritdoc cref="AOeTaskFilter"/>
    [Serializable]
    public abstract class AOeTaskFilterAttributes : AOeTaskFilter {

        /// <inheritdoc cref="AOeTaskFilter.Include"/>
        [XmlAttribute(AttributeName = "Include")]
        public override string Include {
            get => base.Include;
            set => base.Include = value;
        }
        
        /// <inheritdoc cref="AOeTaskFilter.IncludeRegex"/>
        [XmlAttribute(AttributeName = "IncludeRegex")]
        public override string IncludeRegex {
            get => base.IncludeRegex;
            set => base.IncludeRegex = value;
        }
        
        /// <inheritdoc cref="AOeTaskFilter.Exclude"/>
        [XmlAttribute(AttributeName = "Exclude")]
        public override string Exclude {
            get => base.Exclude;
            set => base.Exclude = value;
        }
        
        /// <inheritdoc cref="AOeTaskFilter.ExcludeRegex"/>
        [XmlAttribute(AttributeName = "ExcludeRegex")]
        public override string ExcludeRegex {
            get => base.ExcludeRegex;
            set => base.ExcludeRegex = value;
        }
    }
}