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
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    [Serializable]
    [XmlRoot("RemoveDirectory")]
    public class OeTaskRemoveDirectory : OeTask {
        
        [XmlAttribute("DirectoryPath")]
        public string DirectoryPath { get; set; }
        
        public override void Validate() {
            if (string.IsNullOrEmpty(DirectoryPath) && string.IsNullOrEmpty(DirectoryPath)) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined or it will not do anything : {GetType().GetXmlName(nameof(DirectoryPath))}");
            }
            base.Validate();
        }

        protected override void ExecuteInternal() {
            CancelSource?.Token.ThrowIfCancellationRequested();
            if (Directory.Exists(DirectoryPath)) {
                Log?.Trace?.Write($"Deleting directory {DirectoryPath.PrettyQuote()}");
                try {
                    Directory.Delete(DirectoryPath, true);
                } catch (Exception e) {
                    throw new TaskExecutionException(this, $"Could not delete directory {DirectoryPath.PrettyQuote()}", e);
                }
            } else {
                Log?.Trace?.Write($"Deleting directory not existing {DirectoryPath}");
            }
        }

    }
}