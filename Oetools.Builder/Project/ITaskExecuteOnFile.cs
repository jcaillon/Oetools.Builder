﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IOeTaskOnFile.cs) is part of Oetools.Builder.
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
using Oetools.Builder.History;

namespace Oetools.Builder.Project {
    public interface ITaskExecuteOnFile {

        /// <summary>
        /// Returns true if the file is included in this task
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        bool IsFileIncluded(OeFile file);
        
        /// <summary>
        /// Returns true of the file is excluded in this task
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        bool IsFileExcluded(OeFile file);
        
        void ExecuteForFile(OeFile file);

        List<OeFileBuilt> GetFilesBuilt();

        List<string> GetIncludeRegexStrings();

        List<string> GetExcludeRegexStrings();
        
        List<string> GetIncludeOriginalStrings();
        
        List<string> GetExcludeOriginalStrings();
    }
}