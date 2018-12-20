#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTarget.cs) is part of Oetools.Builder.
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
using System.IO;
using System.Xml.Serialization;
using Oetools.Utilities.Archive;

namespace Oetools.Builder.History {
    
    public abstract class AOeTarget {
        
        /// <summary>
        /// Path to the archive file.
        /// </summary>
        [XmlIgnore]
        public abstract string ArchiveFilePath { get; set; }
        
        /// <summary>
        /// The file path inside the archive (relative).
        /// </summary>
        [XmlIgnore]
        public abstract string FilePathInArchive { get; set; }

        public virtual string GetTargetPath() => Path.Combine(ArchiveFilePath, FilePathInArchive);
        
        /// <summary>
        /// Returns an archiver instance capable of checking the existence of the given target type.
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        internal static IArchiverExistenceCheck GetArchiverExistenceCheck(Type targetType) {
            return GetArchiver(targetType);
        }
        
        /// <summary>
        /// Returns an archiver instance capable of deleting the given target type.
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        internal static IArchiverDelete GetArchiverDelete(Type targetType) {
            return GetArchiver(targetType);
        }
        
        /// <summary>
        /// Returns an archiver for the given target type.
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        internal static IArchiverFullFeatured GetArchiver(Type targetType) {
            if (targetType == typeof(OeTargetFile)) {
                return Archiver.NewFileSystemArchiver();
            }
            if (targetType == typeof(OeTargetProlib)) {
                return Archiver.NewProlibArchiver();
            }
            if (targetType == typeof(OeTargetCab)) {
                return Archiver.NewCabArchiver();
            }
            if (targetType == typeof(OeTargetZip)) {
                return Archiver.NewZipArchiver();
            }
            if (targetType == typeof(OeTargetFtp)) {
                return Archiver.NewFtpArchiver();
            }
            return null;
        }
    }
}