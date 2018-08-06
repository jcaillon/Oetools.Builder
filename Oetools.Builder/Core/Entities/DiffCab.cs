// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (DiffCab.cs) is part of Oetools.Builder.
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

using System.Collections.Generic;

namespace Oetools.Builder.Core.Entities {
    public class DiffCab {
        /// <summary>
        ///     1
        /// </summary>
        public int VersionToUpdateFrom { get; set; }

        /// <summary>
        ///     2
        /// </summary>
        public int VersionToUpdateTo { get; set; }

        /// <summary>
        ///     $TARGET/wcp/new-10-02/diffs/new1to2.cab
        /// </summary>
        public string CabPath { get; set; }

        /// <summary>
        ///     $REFERENCE/wcp/new-10-02/diffs/new1to2.cab
        /// </summary>
        public string ReferenceCabPath { get; set; }

        /// <summary>
        ///     $TARGET/wcp/new-10-02/diffs/new1to2
        /// </summary>
        internal string TempCabFolder { get; set; }

        /// <summary>
        ///     List of all the files that were deployed in the clientNWK since this VersionToUpdateFrom
        /// </summary>
        public List<FileDeployed> FilesDeployedInNwkSincePreviousVersion { get; set; }

        /// <summary>
        ///     List of all the files to pack (for step 1, which is we move/cab files into a temporary directory for this diff)
        /// </summary>
        public List<FileToDeploy> FilesToPackStep1 { get; set; }
    }
}