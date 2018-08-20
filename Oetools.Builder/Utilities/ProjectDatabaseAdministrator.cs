#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ProjectDatabaseAdministrator.cs) is part of Oetools.Builder.
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
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Database;

namespace Oetools.Builder.Utilities {
    
    public class ProjectDatabaseAdministrator : IDisposable {

        public string SourceDirectory { get; }
        
        public OeProjectProperties Properties { get; }
        
        public UoeDatabaseAdministrator DbAdmin { get; }
        
        public string GetProjectDatabaseDirectory => OeBuilderConstants.GetProjectDirectoryLocalDb(SourceDirectory);

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="properties"></param>
        /// <exception cref="ProjectDatabaseAdministratorException"></exception>
        public ProjectDatabaseAdministrator(string sourceDirectory, OeProjectProperties properties) {
            SourceDirectory = sourceDirectory;
            Properties = properties;
            try {
                DbAdmin = new UoeDatabaseAdministrator(properties?.DlcDirectoryPath.TakeDefaultIfNeeded(OeProjectProperties.GetDefaultDlcDirectoryPath()));
            } catch (Exception e) {
                throw new ProjectDatabaseAdministratorException($"Error initiating the database administrator for the projet : {e.Message}", e);
            }
        }

        public void Dispose() {
            DbAdmin?.Dispose();
        }
        
        public List<string> GetDatabaseConnectionStringsAfterCreationOrUpdateIfNeeded() {
            // TODO
            return null;
        }
    }
}