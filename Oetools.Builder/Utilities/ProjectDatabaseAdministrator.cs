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
using System.IO;
using System.Text;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Database;

namespace Oetools.Builder.Utilities {
    
    public class ProjectDatabaseAdministrator : IDisposable {
        
        public ILogger Log { get; set; }

        public string SourceDirectory { get; }
        
        public int ConfigurationId { get; }
        
        public OeProjectProperties Properties { get; }
        
        public UoeDatabaseAdministrator DbAdmin { get; }
        
        public string ProjectDatabaseDirectory => OeBuilderConstants.GetProjectDirectoryLocalDb(SourceDirectory);

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="sourceDirectory"></param>
        /// <exception cref="ProjectDatabaseAdministratorException"></exception>
        public ProjectDatabaseAdministrator(OeBuildConfiguration configuration, string sourceDirectory) {
            if (string.IsNullOrEmpty(sourceDirectory)) {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            if (!Directory.Exists(sourceDirectory)) {
                throw new ProjectDatabaseAdministratorException($"The source directory must exist : {sourceDirectory}");
            }
            SourceDirectory = sourceDirectory;
            Properties = configuration.Properties;
            ConfigurationId = configuration.Id;
            try {
                DbAdmin = new UoeDatabaseAdministrator(Properties?.DlcDirectoryPath.TakeDefaultIfNeeded(OeProjectProperties.GetDefaultDlcDirectoryPath()));
            } catch (Exception e) {
                throw new ProjectDatabaseAdministratorException($"Error initiating the database administrator for the projet : {e.Message}", e);
            }
        }

        public void Dispose() {
            DbAdmin?.Dispose();
        }
        
        /// <summary>
        /// Ensures that all the databases defined are correctly defined
        /// </summary>
        /// <exception cref="ProjectDatabaseAdministratorException"></exception>
        public void ValidateProjectDatabases() {
            var uniqueLogicalNames = new HashSet<string>();
            foreach (var db in (Properties?.ProjectDatabases).ToNonNullList()) {
                if (!uniqueLogicalNames.Add(db.LogicalName)) {
                    throw new ProjectDatabaseAdministratorException($"The logical database name {db.LogicalName} is defined twice, it should be unique");
                }
                UoeDatabaseOperator.ValidateLogicalName(db.LogicalName);
                if (!File.Exists(db.DataDefinitionFilePath)) {
                    throw new ProjectDatabaseAdministratorException($"The data definition file for the database {db.LogicalName} does not exist {db.DataDefinitionFilePath}");
                }
            }
        }

        /// <summary>
        /// Returns true if the databases created and started by this class should be shutdown on build done
        /// </summary>
        /// <returns></returns>
        public bool ShouldShutdownCompilationDatabasesAfterBuild() => Properties?.ShutdownCompilationDatabasesAfterBuild ?? OeProjectProperties.GetDefaultShutdownCompilationDatabasesAfterBuild();
        
        /// <summary>
        /// Sets up all the databases needed for the project, starts them and returns the needed connection strings (or null if no db)
        /// </summary>
        /// <returns></returns>
        public List<string> SetupProjectDatabases() {
            if (Properties?.ProjectDatabases == null || Properties.ProjectDatabases.Count == 0) {
                return null;
            }
            ValidateProjectDatabases();
            var nbProcessesToUse = OeCompilationOptions.GetNumberOfProcessesToUse(Properties?.CompilationOptions);
            DeleteOutdatedDatabases();
            CreateInexistingProjectDatabases();
            ServeUnstartedDatabases(nbProcessesToUse);
            return GetDatabasesConnectionStrings(nbProcessesToUse > 1);
        }
        
        /// <summary>
        /// Returns all the connection strings for the project databases
        /// </summary>
        /// <param name="multiUserConnection"></param>
        /// <returns></returns>
        public List<string> GetDatabasesConnectionStrings(bool? multiUserConnection = null) {
            var output = new List<string>();
            foreach (var db in (Properties?.ProjectDatabases).ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);
                if (multiUserConnection == null && DbAdmin.GetBusyMode(dbPath) == DatabaseBusyMode.MultiUser || multiUserConnection.HasValue && multiUserConnection.Value) {
                    output.Add(UoeDatabaseOperator.GetMultiUserConnectionString(dbPath, logicalName: db.LogicalName));
                } else {
                    output.Add(UoeDatabaseOperator.GetSingleUserConnectionString(dbPath, db.LogicalName));
                }}
            return output;
        }
        
        /// <summary>
        /// Starts all the project databases
        /// </summary>
        /// <param name="nbUsers"></param>
        public void ServeUnstartedDatabases(int? nbUsers = null) {
            foreach (var db in (Properties?.ProjectDatabases).ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);
                var maxNbUsersFile = GetMaxNbUsersFileFromDatabaseFilePath(dbPath);
                int currentMaxNbUsers = 0;
                if (File.Exists(maxNbUsersFile)) {
                    int.TryParse(File.ReadAllText(maxNbUsersFile), out currentMaxNbUsers);
                }
                var busyMode = DbAdmin.GetBusyMode(dbPath);
                
                if (busyMode != DatabaseBusyMode.MultiUser || currentMaxNbUsers != (nbUsers ?? 20)) {
                    if (busyMode == DatabaseBusyMode.MultiUser) {
                        Log?.Debug($"Shutting down database {dbPath}, the number of max users has changed from {currentMaxNbUsers} to {nbUsers ?? 20}");
                        DbAdmin.Proshut(dbPath);
                    }
                    Log?.Debug($"Starting database {dbPath} for {nbUsers ?? 20} max users");
                    DbAdmin.ProServe(dbPath, nbUsers);
                    File.WriteAllText(maxNbUsersFile, (nbUsers ?? 20).ToString());
                 } else {
                    Log?.Debug($"The database {dbPath} is already started with the right number of max users");
                }
            }
        }        
        
        /// <summary>
        /// Shutdown all the project databases
        /// </summary>
        public void ShutdownAllDatabases() {
            foreach (var db in (Properties?.ProjectDatabases).ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);
                Log?.Debug($"shutting down the database {dbPath}");
                DbAdmin.Proshut(dbPath);
            }
        }

        /// <summary>
        /// Delete all the outdated databases of the project; a database is outdated if the .df file that was used to
        /// create it is different than the current .df (MD5 hash used)
        /// </summary>
        public void DeleteOutdatedDatabases() {
            foreach (var db in (Properties?.ProjectDatabases).ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);
                var md5Path = GetMd5FileFromDatabaseFilePath(dbPath);
                
                if (File.Exists(md5Path) && File.ReadAllText(md5Path, Encoding.Default).Equals(GetHashFromDfFile(db.DataDefinitionFilePath))) {
                    Log?.Debug($"The database {dbPath} is up to date");
                    continue;
                }
                
                if (File.Exists(dbPath)) {
                    if (DbAdmin.GetBusyMode(dbPath) != DatabaseBusyMode.NotBusy) {
                        Log?.Debug($"Shutting down database {dbPath} before deletion");
                        DbAdmin.Proshut(dbPath);
                    }
                    
                    Log?.Debug($"Deleting the database {dbPath}");
                    DbAdmin.Delete(dbPath);
                }
                Utils.DeleteDirectoryIfExists(Path.GetDirectoryName(dbPath), true);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void CreateInexistingProjectDatabases() {
            foreach (var db in (Properties?.ProjectDatabases).ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);

                if (!File.Exists(dbPath)) {
                    Log?.Debug($"Creating a new database {dbPath} for the logical name {db.LogicalName} with the data definition file {db.DataDefinitionFilePath}");
                    DbAdmin.CreateCompilationDatabaseFromDf(dbPath, db.DataDefinitionFilePath);

                    // write a file next to the database which identifies uniquely the .df file that was used to create the db
                    File.WriteAllText(GetMd5FileFromDatabaseFilePath(dbPath), GetHashFromDfFile(db.DataDefinitionFilePath), Encoding.Default);
                } else {
                    Log?.Debug($"The database {dbPath} for the logical name {db.LogicalName} already exists");
                }
            }
        }

        private string GetDatabasePathFromPhysicalName(string physicalName) {
            return Path.Combine(ProjectDatabaseDirectory, ConfigurationId.ToString(), physicalName, $"{physicalName}.db");
        }

        private string GetMaxNbUsersFileFromDatabaseFilePath(string dbPath) {
            return Path.ChangeExtension(dbPath, "maxnbusers");
        }

        private string GetMd5FileFromDatabaseFilePath(string dbPath) {
            return Path.ChangeExtension(dbPath, "md5");
        }
        
        private string GetPhysicalNameFromLogicalName(string logicalName) {
            return UoeDatabaseOperator.GetValidPhysicalName(logicalName);
        }

        private string GetHashFromDfFile(string filePath) {
            return Utils.GetMd5FromFilePath(filePath);
        }

    }
}