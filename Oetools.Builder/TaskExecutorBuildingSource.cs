﻿// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutor.cs) is part of Oetools.Builder.
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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
       
    public class TaskExecutorBuildingSource : TaskExecutorWithFileList, IDisposable {
        
        public List<OeFileBuilt> PreviouslyBuiltFiles { get; set; }
        
        public bool ForceFullRebuild { get; set; }
        
        public TaskExecutorBuildingSource(List<OeTask> tasks) : base(tasks) { }
        
        public void Dispose() {
              
        }
    }
}