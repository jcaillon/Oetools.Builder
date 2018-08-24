#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeFileBuilt.cs) is part of Oetools.Builder.
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
using System.Linq;
using System.Xml.Serialization;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Attributes;

namespace Oetools.Builder.History {
    [Serializable]
    public class OeFileBuilt : OeFile {
        
        public OeFileBuilt() { }

        public OeFileBuilt(OeFile sourceFile) {
            Utils.DeepCopyPublicProperties(sourceFile, GetType(), this);
        }

        /// <summary>
        /// A list of the targets for this file
        /// </summary>
        [DeepCopy(Ignore = true)]
        [XmlArray("Targets")]
        [XmlArrayItem("Copied", typeof(OeTargetFileCopy))]
        [XmlArrayItem("Prolibed", typeof(OeTargetArchiveProlib))]
        [XmlArrayItem("Zipped", typeof(OeTargetArchiveZip))]
        [XmlArrayItem("Cabbed", typeof(OeTargetArchiveCab))]
        public List<OeTarget> Targets { get; set; }

        public override IEnumerable<OeTarget> GetAllTargets() => Targets;
    }
}