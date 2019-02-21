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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project.Properties;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Database;
using Oetools.Utilities.Openedge.Database.Exceptions;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Utilities {

    /// <summary>
    /// Allows to administrate the databases of a project.
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
        /// The encoding to use for I/O of the openedge executables.
        /// </summary>
        public Encoding Encoding { get; }

        /// <summary>
        /// The logger to use.
        /// </summary>
        public ILogger Log {
            get => _logger;
            set {
                _logger = value;
                DbAdmin.Log = _logger;
            }
        }

        /// <summary>
        /// Cancellation token. Used to cancel execution.
        /// </summary>
        public CancellationToken? CancelToken {
            get => DbAdmin.CancelToken;
            set => DbAdmin.CancelToken = value;
        }

        /// <summary>
        /// Pro parameters to append to the execution of the progress process.
        /// </summary>
        public UoeProcessArgs ProExeCommandLineParameters {
            get => DbAdmin.ProExeCommandLineParameters;
            set => DbAdmin.ProExeCommandLineParameters = value;
        }

        /// <summary>
        /// Database server internationalization startup parameters such as -cpinternal codepage and -cpstream codepage.
        /// They will be used for commands that support them. (_dbutil, _mprosrv, _mprshut, _proutil)
        /// </summary>
        /// <remarks>
        /// https://documentation.progress.com/output/ua/OpenEdge_latest/index.html#page/dmadm%2Fdatabase-server-internationalization-parameters.html%23
        /// </remarks>
        public ProcessArgs InternationalizationStartupParameters {
            get => DbAdmin.InternationalizationStartupParameters;
            set => DbAdmin.InternationalizationStartupParameters = value;
        }

        /// <summary>
        /// The databases to handle.
        /// </summary>
        protected List<ProjectDatabase> ProjectDatabases { get; }

        protected UoeDatabaseAdministrator DbAdmin { get; }

        private ILogger _logger;

        /// <summary>
        /// Initialize a project database administrator.
        /// </summary>
        /// <param name="dlcDirectory"></param>
        /// <param name="projectDatabases"></param>
        /// <param name="projectDatabaseDirectory">The base directory in which to store the project databases (1 subdir for each db).</param>
        /// <param name="encoding"></param>
        /// <exception cref="ProjectDatabaseAdministratorException"></exception>
        /// <exception cref="UoeDatabaseException"></exception>
        public ProjectDatabaseAdministrator(string dlcDirectory, List<OeProjectDatabase> projectDatabases, string projectDatabaseDirectory, Encoding encoding = null) {
            if (string.IsNullOrEmpty(projectDatabaseDirectory)) {
                throw new ArgumentNullException(nameof(projectDatabaseDirectory));
            }
            if (!Directory.Exists(projectDatabaseDirectory)) {
                Directory.CreateDirectory(projectDatabaseDirectory);
            }
            Encoding = encoding;
            try {
                DbAdmin = new UoeDatabaseAdministrator(dlcDirectory, encoding);
            } catch (Exception e) {
                throw new ProjectDatabaseAdministratorException($"Error initiating the database administrator for the project: {e.Message}.", e);
            }
            ValidateProjectDatabases(projectDatabases);
            ProjectDatabases = projectDatabases?.Select(p => new ProjectDatabase(p, projectDatabaseDirectory)).ToList() ?? new List<ProjectDatabase>();
        }

        public void Dispose() {
            DbAdmin?.Dispose();
        }

        /// <summary>
        /// Ensures that all the databases defined are correctly defined.
        /// </summary>
        /// <exception cref="ProjectDatabaseAdministratorException"></exception>
        public void ValidateProjectDatabases(List<OeProjectDatabase> projectDatabases) {
            var uniqueLogicalNames = new HashSet<string>();
            foreach (var db in projectDatabases) {
                UoeDatabaseLocation.ValidateLogicalName(db.LogicalName);
                if (!uniqueLogicalNames.Add(db.LogicalName)) {
                    throw new ProjectDatabaseAdministratorException($"The logical database name {db.LogicalName.PrettyQuote()} is defined twice, it should be unique.");
                }
                if (!File.Exists(db.DataDefinitionFilePath)) {
                    throw new ProjectDatabaseAdministratorException($"The data definition file for the database {db.LogicalName.PrettyQuote()} does not exist {db.DataDefinitionFilePath.PrettyQuote()}.");
                }
            }
        }

        /// <summary>
        /// Sets up all the databases needed for the project, starts them and returns the needed connection strings (or null if no db)
        /// </summary>
        /// <returns></returns>
        public List<UoeDatabaseConnection> SetupProjectDatabases() {
            if (ProjectDatabases == null || ProjectDatabases.Count == 0) {
                return null;
            }
            var nbUsers = NumberOfUsersPerDatabase ?? DefaultNumberOfUsersPerDatabase;
            DeleteOutdatedDatabases();
            CreateInexistingProjectDatabases();
            ServeUnstartedDatabases(nbUsers);
            return GetDatabasesConnectionStrings(nbUsers > 1);
        }

        /// <summary>
        /// Returns all the connection strings for the project databases
        /// </summary>
        /// <param name="multiUserConnection"></param>
        /// <returns></returns>
        public List<UoeDatabaseConnection> GetDatabasesConnectionStrings(bool? multiUserConnection = null) {
            var output = new List<UoeDatabaseConnection>();
            foreach (var db in ProjectDatabases) {
                if (multiUserConnection == null && DbAdmin.GetBusyMode(db.Location) == DatabaseBusyMode.MultiUser || multiUserConnection.HasValue && multiUserConnection.Value) {
                    output.Add(UoeDatabaseConnection.NewMultiUserConnection(db.Location, db.Definition.LogicalName));
                } else {
                    output.Add(UoeDatabaseConnection.NewSingleUserConnection(db.Location, db.Definition.LogicalName));
                }}
            return output;
        }

        /// <summary>
        /// Starts all the project databases
        /// </summary>
        /// <param name="numberOfUsersPerDatabase"></param>
        public void ServeUnstartedDatabases(int? numberOfUsersPerDatabase = null) {
            int nbUsers = numberOfUsersPerDatabase ?? DefaultNumberOfUsersPerDatabase;
            foreach (var db in ProjectDatabases) {
                int currentMaxNbUsers = db.MaxNumberOfUsers;
                var busyMode = DbAdmin.GetBusyMode(db.Location);

                if (busyMode != DatabaseBusyMode.MultiUser || currentMaxNbUsers != nbUsers) {
                    if (busyMode == DatabaseBusyMode.MultiUser) {
                        Log?.Debug($"Shutting down database {db.ToString().PrettyQuote()}, the number of max users has changed from {currentMaxNbUsers} to {nbUsers}.");
                        ShutdownDatabase(db);
                    }
                    if (nbUsers > 1) {
                        Log?.Debug($"Starting database {db.ToString().PrettyQuote()} for {nbUsers} max users.");
                        DbAdmin.Start(db.Location, nbUsers, out List<int> pids);

                        if (pids.Count == 1) {
                            Log?.Debug($"Database started with process id {pids[0]}.");
                            db.BrokerProcessPid = pids[0];
                        }
                    } else {
                        Log?.Debug("Single user mode, the database will not be started.");
                    }

                    db.MaxNumberOfUsers = nbUsers;
                } else {
                    Log?.Debug($"The database {db.ToString().PrettyQuote()} is already started with the right number of max users.");
                }
            }
        }

        /// <summary>
        /// Shutdown all the project databases
        /// </summary>
        public void ShutdownAllDatabases() {
            foreach (var db in ProjectDatabases) {
                Log?.Debug($"Shutting down the database {db}.");
                ShutdownDatabase(db);
            }
        }

        /// <summary>
        /// Delete all the outdated databases of the project; a database is outdated if the .df file that was used to
        /// create it is different than the current .df (MD5 hash used)
        /// </summary>
        public void DeleteOutdatedDatabases() {
            foreach (var db in ProjectDatabases.ToNonNullEnumerable()) {
                var dfHash = db.GetLocalDfHash();
                if (!string.IsNullOrEmpty(dfHash) && dfHash.Equals(db.GetDefinitionDfHash())) {
                    Log?.Debug($"The database {db.ToString().PrettyQuote()} is up to date.");
                    continue;
                }

                if (db.Location.Exists()) {
                    if (DbAdmin.GetBusyMode(db.Location) != DatabaseBusyMode.NotBusy) {
                        Log?.Debug($"Shutting down database {db.ToString().PrettyQuote()} before deletion.");
                        ShutdownDatabase(db);
                    }

                    Log?.Debug($"Deleting the database {db.ToString().PrettyQuote()}.");
                    DbAdmin.Delete(db.Location);
                }
                Utils.DeleteDirectoryIfExists(db.Location.DirectoryPath, true);
            }
        }

        /// <summary>
        /// Create needed databases that do not exist yet.
        /// </summary>
        public void CreateInexistingProjectDatabases() {
            foreach (var db in ProjectDatabases.ToNonNullEnumerable()) {
                if (!db.Location.Exists()) {
                    Log?.Debug($"Creating a new database {db.ToString().PrettyQuote()} for the logical name {db.Definition.LogicalName} with the data definition file {db.Definition.DataDefinitionFilePath.PrettyQuote()}.");

                    if (!Directory.Exists(db.Location.DirectoryPath)) {
                        Directory.CreateDirectory(db.Location.DirectoryPath);
                    }

                    Log?.Debug($"Copying data definition file {db.Definition.DataDefinitionFilePath.PrettyQuote()} to {db.LocalDfFilePath.PrettyQuote()}.");
                    File.Copy(db.Definition.DataDefinitionFilePath, db.LocalDfFilePath);

                    DbAdmin.CreateWithDf(db.Location, db.LocalDfFilePath);

                    // write a file next to the database which identifies uniquely the .df file that was used to create the db
                    db.SaveLocalDfHash();
                } else {
                    Log?.Debug($"The database {db.ToString().PrettyQuote()} for the logical name {db.Definition.LogicalName} already exists, nothing needs to be done.");
                }
            }
        }

        private void ShutdownDatabase(ProjectDatabase db) {
            Log?.Debug($"Shutting down database {db.ToString().PrettyQuote()}.");
            if (AllowsDatabaseShutdownWithKill ?? DefaultAllowsDatabaseShutdownWithKill) {
                var dbBrokerProcessPid = db.BrokerProcessPid;
                if (dbBrokerProcessPid.HasValue) {
                    var pid = dbBrokerProcessPid.Value;
                    try {
                        if (DbAdmin.KillBrokerServer(pid, db.Location)) {
                            return;
                        }
                    } catch (Exception e) {
                        Log?.Warn($"Failed to shutdown the database using process kill: {db.ToString().PrettyQuote()}.", e);
                    }
                }
            }
            DbAdmin.Shutdown(db.Location);
        }



    }
}
