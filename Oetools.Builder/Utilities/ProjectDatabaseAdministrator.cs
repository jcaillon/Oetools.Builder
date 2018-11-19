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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Database;

namespace Oetools.Builder.Utilities {
    
    /// <summary>
    /// Allows to administrate the databases of a build configuration.
    /// </summary>
    public class ProjectDatabaseAdministrator : IDisposable {

        private const int DefaultNumberOfUsersPerDatabase = 20;
        private const bool DefaultAllowsDatabaseShutdownWithKill = true;
        
        /// <summary>
        /// The max number of users for each database (will impact the start up parameters).
        /// </summary>
        public int? NumberOfUsersPerDatabase { get; set; }

        /// <summary>
        /// Are we allowed to use a process kill instead of a classic proshut?
        /// </summary>
        public bool? AllowsDatabaseShutdownWithKill { get; set; }
        
        /// <summary>
        /// The logger to use.
        /// </summary>
        public ILogger Log { protected get; set; }
        
        /// <summary>
        /// The databases to handle.
        /// </summary>
        protected List<OeProjectDatabase> ProjectDatabases { get; }
        
        /// <summary>
        /// The base directory where to store the databases.
        /// </summary>
        protected string ProjectDatabaseDirectory { get; }
        
        protected UoeDatabaseAdministrator DbAdmin { get; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dlcDirectory"></param>
        /// <param name="projectDatabases"></param>
        /// <param name="projectDatabaseDirectory"></param>
        /// <exception cref="ProjectDatabaseAdministratorException"></exception>
        public ProjectDatabaseAdministrator(string dlcDirectory, List<OeProjectDatabase> projectDatabases, string projectDatabaseDirectory) {
            if (string.IsNullOrEmpty(projectDatabaseDirectory)) {
                throw new ArgumentNullException(nameof(projectDatabaseDirectory));
            }
            if (!Directory.Exists(projectDatabaseDirectory)) {
                Directory.CreateDirectory(projectDatabaseDirectory);
            }
            ProjectDatabases = projectDatabases;
            ProjectDatabaseDirectory = projectDatabaseDirectory;
            try {
                DbAdmin = new UoeDatabaseAdministrator(dlcDirectory);
            } catch (Exception e) {
                throw new ProjectDatabaseAdministratorException($"Error initiating the database administrator for the project: {e.Message}.", e);
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
            foreach (var db in ProjectDatabases.ToNonNullList()) {
                if (!uniqueLogicalNames.Add(db.LogicalName)) {
                    throw new ProjectDatabaseAdministratorException($"The logical database name {db.LogicalName.PrettyQuote()} is defined twice, it should be unique.");
                }
                UoeDatabaseOperator.ValidateLogicalName(db.LogicalName);
                if (!File.Exists(db.DataDefinitionFilePath)) {
                    throw new ProjectDatabaseAdministratorException($"The data definition file for the database {db.LogicalName.PrettyQuote()} does not exist {db.DataDefinitionFilePath.PrettyQuote()}.");
                }
            }
        }

        /// <summary>
        /// Sets up all the databases needed for the project, starts them and returns the needed connection strings (or null if no db)
        /// </summary>
        /// <returns></returns>
        public List<string> SetupProjectDatabases() {
            if (ProjectDatabases == null || ProjectDatabases.Count == 0) {
                return null;
            }
            ValidateProjectDatabases();
            var nbProcessesToUse = NumberOfUsersPerDatabase ?? DefaultNumberOfUsersPerDatabase;
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
            foreach (var db in ProjectDatabases.ToNonNullList()) {
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
        /// <param name="numberOfUsersPerDatabase"></param>
        public void ServeUnstartedDatabases(int? numberOfUsersPerDatabase = null) {
            int nbUsers = numberOfUsersPerDatabase ?? DefaultNumberOfUsersPerDatabase;
            foreach (var db in ProjectDatabases.ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);
                var maxNbUsersFile = GetMaxNbUsersFileFromDatabaseFilePath(dbPath);
                var startTimeFile = GetProcessStartTimeFileFromDatabaseFilePath(dbPath);
                int currentMaxNbUsers = 0;
                if (File.Exists(maxNbUsersFile)) {
                    int.TryParse(File.ReadAllText(maxNbUsersFile), out currentMaxNbUsers);
                }
                var busyMode = DbAdmin.GetBusyMode(dbPath);
                
                if (busyMode != DatabaseBusyMode.MultiUser || currentMaxNbUsers != nbUsers) {
                    if (busyMode == DatabaseBusyMode.MultiUser) {
                        Log?.Debug($"Shutting down database {dbPath.PrettyQuote()}, the number of max users has changed from {currentMaxNbUsers} to {nbUsers}.");
                        ShutdownDatabase(dbPath);
                    }
                    var startTime = DateTime.Now;

                    Log?.Debug($"Starting database {dbPath} for {nbUsers} max users.");
                    var startParameters = DbAdmin.ProServe(dbPath, null, null, nbUsers);
                    Log?.Debug($"Startup parameters are: {startParameters.PrettyQuote()}.");

                    var newProcess = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.Contains("_mprosrv") && p.StartTime.CompareTo(startTime) > 0);
                    if (newProcess != null) {
                        Log?.Debug($"Database started with process id {newProcess.Id}.");
                        File.WriteAllText(startTimeFile, startTime.ToString("yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture));
                    }

                    File.WriteAllText(maxNbUsersFile, nbUsers.ToString());
                } else {
                    Log?.Debug($"The database {dbPath.PrettyQuote()} is already started with the right number of max users.");
                }
            }
        }        
        
        /// <summary>
        /// Shutdown all the project databases
        /// </summary>
        public void ShutdownAllDatabases() {
            foreach (var db in ProjectDatabases.ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);
                Log?.Debug($"Shutting down the database {dbPath}.");
                ShutdownDatabase(dbPath);
            }
        }

        /// <summary>
        /// Delete all the outdated databases of the project; a database is outdated if the .df file that was used to
        /// create it is different than the current .df (MD5 hash used)
        /// </summary>
        public void DeleteOutdatedDatabases() {
            foreach (var db in ProjectDatabases.ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);
                var md5Path = GetMd5FileFromDatabaseFilePath(dbPath);
                
                if (File.Exists(md5Path) && File.ReadAllText(md5Path, Encoding.Default).Equals(GetHashFromDfFile(db.DataDefinitionFilePath))) {
                    Log?.Debug($"The database {dbPath} is up to date.");
                    continue;
                }
                
                if (File.Exists(dbPath)) {
                    if (DbAdmin.GetBusyMode(dbPath) != DatabaseBusyMode.NotBusy) {
                        Log?.Debug($"Shutting down database {dbPath.PrettyQuote()} before deletion.");
                        ShutdownDatabase(dbPath);
                    }
                    
                    Log?.Debug($"Deleting the database {dbPath.PrettyQuote()}.");
                    DbAdmin.Delete(dbPath);
                }
                Utils.DeleteDirectoryIfExists(Path.GetDirectoryName(dbPath), true);
            }
        }
        
        /// <summary>
        /// Create needed databases that do not exist yet.
        /// </summary>
        public void CreateInexistingProjectDatabases() {
            foreach (var db in ProjectDatabases.ToNonNullList()) {
                var dbName = GetPhysicalNameFromLogicalName(db.LogicalName);
                var dbPath = GetDatabasePathFromPhysicalName(dbName);

                if (!File.Exists(dbPath)) {
                    Log?.Debug($"Creating a new database {dbPath.PrettyQuote()} for the logical name {db.LogicalName} with the data definition file {db.DataDefinitionFilePath.PrettyQuote()}.");
                    DbAdmin.CreateCompilationDatabaseFromDf(dbPath, db.DataDefinitionFilePath);

                    // write a file next to the database which identifies uniquely the .df file that was used to create the db
                    File.WriteAllText(GetMd5FileFromDatabaseFilePath(dbPath), GetHashFromDfFile(db.DataDefinitionFilePath), Encoding.Default);
                } else {
                    Log?.Debug($"The database {dbPath.PrettyQuote()} for the logical name {db.LogicalName} already exists.");
                }
            }
        }

        private void ShutdownDatabase(string dbPath) {
            if (AllowsDatabaseShutdownWithKill ?? DefaultAllowsDatabaseShutdownWithKill) {
                var startTimeFile = GetProcessStartTimeFileFromDatabaseFilePath(dbPath);
                if (File.Exists(startTimeFile)) {
                    var startTimeText = File.ReadAllText(startTimeFile);
                    if (DateTime.TryParseExact(startTimeText, "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime time)) {
                        try {
                            var processToKill = Process.GetProcesses()
                                .Where(p => {
                                    try {
                                        return p.ProcessName.Contains("_mprosrv") && p.StartTime.CompareTo(time) > 0 && p.StartTime.CompareTo(time.AddSeconds(10)) < 0;
                                    } catch(Exception) {
                                        return false;
                                    }
                                })
                                .OrderBy(p => p.StartTime)
                                .FirstOrDefault();
                            if (processToKill != null) {
                                // because of the way we started the database, we know for sure 
                                processToKill.Kill();
                                return;
                            }
                        } catch (Exception e) {
                            Log?.Warn($"Failed to shutdown the database using process kill: {dbPath.PrettyQuote()}.", e);
                        }
                    }
                }
            }
            DbAdmin.Proshut(dbPath);
        }

        private string GetDatabasePathFromPhysicalName(string physicalName) {
            return Path.Combine(ProjectDatabaseDirectory, physicalName, $"{physicalName}.db");
        }

        private string GetMaxNbUsersFileFromDatabaseFilePath(string dbPath) {
            return Path.ChangeExtension(dbPath, "maxnbusers");
        }

        private string GetMd5FileFromDatabaseFilePath(string dbPath) {
            return Path.ChangeExtension(dbPath, "md5");
        }

        private string GetProcessStartTimeFileFromDatabaseFilePath(string dbPath) {
            return Path.ChangeExtension(dbPath, "starttime");
        }
        
        private string GetPhysicalNameFromLogicalName(string logicalName) {
            return UoeDatabaseOperator.GetValidPhysicalName(logicalName);
        }

        private string GetHashFromDfFile(string filePath) {
            return Utils.GetMd5FromFilePath(filePath);
        }

    }
}