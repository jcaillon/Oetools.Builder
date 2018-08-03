// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ResultDeploymentDifferential.cs) is part of Oetools.Packager.
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

using System.Collections.Generic;

namespace Oetools.Packager.Core.Config {
    public class ResultDeploymentDifferential : ResultDeployment {

        public Dictionary<string, FileSourceInfo> SourceFiles { get; set; }
    }
}