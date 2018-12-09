#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskReflectDeletedSourceFile.cs) is part of Oetools.Builder.
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
    
    /// <summary>
    /// This task that deletes the previous target files of a source file that has been deleted since the last build.
    /// </summary>
    /// <remarks>
    /// This task is only useful in incremental mode, where the history of files built is kept.
    /// </remarks>
    /// <example>
    /// On the first build, the file "A" was compiled and copied to location "/bin".
    /// The file "A" is deleted and a second build is started.
    /// With this task, the compiled file "A" in "/bin/A" will be deleted.
    /// </example>
    [Serializable]
    [XmlRoot("ReflectDeletedSourceFile")]
    public class OeTaskReflectDeletedSourceFile : AOeTaskTargetsRemover{
        
    }
}