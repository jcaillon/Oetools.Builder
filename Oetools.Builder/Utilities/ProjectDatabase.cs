#region header
// ========================================================================
// Copyright (c) 2019 - Julien Caillon (julien.caillon@gmail.com)
// This file (ProjectDatabase.cs) is part of Oetools.Builder.
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
using System.Globalization;
using System.IO;
using Oetools.Builder.Project.Properties;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Openedge.Database;
using Oetools.Utilities.Openedge.Database.Exceptions;

namespace Oetools.Builder.Utilities {

    /// <summary>
    /// A database belonging to the project.
    /// </summary>
    public class ProjectDatabase {

        /// <summary>
        /// Project database definition as it appears in the configuration.
        /// </summary>
        public OeProjectDatabase Definition { get; }

        /// <summary>
        /// The project database location.
        /// </summary>
        public UoeDatabaseLocation Location { get; }

        /// <summary>
        /// The maximum number of users for which this database has been started.
        /// </summary>
        public int MaxNumberOfUsers {
            get {
                int currentMaxNbUsers = 0;
                if (File.Exists(MaxNbUsersFilePath)) {
                    int.TryParse(File.ReadAllText(MaxNbUsersFilePath), out currentMaxNbUsers);
                }
                return currentMaxNbUsers;
            }
            set => File.WriteAllText(MaxNbUsersFilePath, value.ToString());
        }

        /// <summary>
        /// The pid of the broker when the database was started.
        /// </summary>
        public int? BrokerProcessPid {
            get {
                if (File.Exists(ProcessPidFilePath)) {
                    var pidText = File.ReadAllText(ProcessPidFilePath);
                    if (int.TryParse(pidText, out int pid)) {
                        return pid;
                    }
                }
                return null;
            }
            set {
                if (value == null) {
                    if (File.Exists(ProcessPidFilePath)) {
                        File.Delete(ProcessPidFilePath);
                    }
                    return;
                }
                File.WriteAllText(ProcessPidFilePath, value.Value.ToString());
            }
        }

        /// <summary>
        /// The hash of the .df file used in the database definition.
        /// </summary>
        /// <returns></returns>
        public string GetDefinitionDfHash() {
            if (File.Exists(LocalDfFilePath)) {
                return Utils.GetMd5FromFilePath(Definition.DataDefinitionFilePath);
            }
            return null;
        }

        /// <summary>
        /// The hash of the local .df file used to create the database.
        /// </summary>
        /// <returns></returns>
        public string GetLocalDfHash() {
            if (File.Exists(LocalDfFilePath)) {
                return Utils.GetMd5FromFilePath(LocalDfFilePath);
            }
            return null;
        }

        /// <summary>
        /// Saves the hash of the local .df file used to create the database.
        /// </summary>
        public void SaveLocalDfHash() => File.WriteAllText(Md5FilePath, Utils.GetMd5FromFilePath(LocalDfFilePath));

        /// <summary>
        /// The path to the local .df file used to created the database.
        /// </summary>
        public string LocalDfFilePath { get; }

        private string MaxNbUsersFilePath { get; }

        private string Md5FilePath { get; }

        private string ProcessPidFilePath { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="projectDatabaseDirectory"></param>
        /// <exception cref="UoeDatabaseException"></exception>
        public ProjectDatabase(OeProjectDatabase definition, string projectDatabaseDirectory) {
            Definition = definition;
            var physicalName = UoeDatabaseLocation.GetValidPhysicalName(definition.LogicalName);
            Location = new UoeDatabaseLocation(Path.Combine(projectDatabaseDirectory, physicalName, physicalName));
            MaxNbUsersFilePath = Path.ChangeExtension(Location.FullPath, "maxnbusers");
            Md5FilePath = Path.ChangeExtension(Location.FullPath, "md5");
            ProcessPidFilePath = Path.ChangeExtension(Location.FullPath, "pid");
            LocalDfFilePath = Path.ChangeExtension(Location.FullPath, "df");
        }

        public override string ToString() => Location.ToString();
    }
}
