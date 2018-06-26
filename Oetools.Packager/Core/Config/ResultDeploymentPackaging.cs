#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ResultDeploymentPackaging.cs) is part of Oetools.Packager.
// 
// Oetools.Packager is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Packager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Packager. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using Oetools.Packager.Core.Entities;

namespace Oetools.Packager.Core.Config {
    public class ResultDeploymentPackaging : ResultDeploymentDifferential {
        
        public DateTime? PackagingStartTime { get; set; }
        
        public TimeSpan? TotalPackagingTime { get; set; }
        
        /// <summary>
        /// has a webclient .cab been created this time?
        /// </summary>
        public bool WebClientCreated { get; set; }
        
        public bool HasWebClient { get; set; }

        public List<DiffCab> DifferentialCabinets { get; set; }

    }
}