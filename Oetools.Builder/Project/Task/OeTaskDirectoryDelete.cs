#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskRemoveDir.cs) is part of Oetools.Builder.
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
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// This task allows to delete directories.
    /// </summary>
    [Serializable]
    [XmlRoot("DeleteDirectory")]
    public class OeTaskDirectoryDelete : AOeTaskDirectory {

        protected override void ExecuteForDirectoriesInternal(PathList<OeDirectory> directories) {
            throw new NotImplementedException();
            
            //CancelToken?.ThrowIfCancellationRequested();
            //if (Directory.Exists(DirectoryPath)) {
            //    Log?.Trace?.Write($"Deleting directory {DirectoryPath.PrettyQuote()}");
            //    try {
            //        Directory.Delete(DirectoryPath, true);
            //    } catch (Exception e) {
            //        throw new TaskExecutionException(this, $"Could not delete directory {DirectoryPath.PrettyQuote()}", e);
            //    }
            //} else {
            //    Log?.Trace?.Write($"Deleting directory not existing {DirectoryPath}");
            //}
        }

        protected override void ExecuteTestModeInternal() {
            throw new NotImplementedException();
        }
    }
}