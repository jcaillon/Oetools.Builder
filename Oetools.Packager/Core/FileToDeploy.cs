﻿#region header

// ========================================================================
// Copyright (c) 2017 - Julien Caillon (julien.caillon@gmail.com)
// This file (FileToDeploy.cs) is part of csdeployer.
// 
// csdeployer is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// csdeployer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with csdeployer. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;
using System.IO;
using System.Text.RegularExpressions;
using Oetools.Utilities;
using Oetools.Utilities.Compression;
using Oetools.Utilities.Compression.Cab;
using Oetools.Utilities.Compression.Prolib;
using Oetools.Utilities.Compression.Zip;
using Oetools.Utilities.Ftp;
using Oetools.Utilities.Lib;

namespace Oetools.Packager.Core {
    /// <summary>
    ///     Represents a file that needs to be deployed
    /// </summary>
    public class FileToDeploy {
        #region Life and death

        /// <summary>
        ///     Constructor
        /// </summary>
        public FileToDeploy(string sourcePath, string targetBasePath, DeployTransferRule rule) {
            Origin = sourcePath;
            TargetBasePath = targetBasePath;
            RuleReference = rule;
        }

        #endregion

        #region Protected methods

        /// <summary>
        ///     Deploy this file
        /// </summary>
        protected virtual bool TryDeploy() {
            return true;
        }

        #endregion

        #region Factory

        public static FileToDeploy New(DeployType deployType, string sourcePath, string targetBasePath, DeployTransferRule rule) {
            switch (deployType) {
                case DeployType.Prolib:
                    return new FileToDeployProlib(sourcePath, targetBasePath, rule);
                case DeployType.Zip:
                    return new FileToDeployZip(sourcePath, targetBasePath, rule);
                case DeployType.DeleteInProlib:
                    return new FileToDeployDeleteInProlib(sourcePath, targetBasePath, rule);
                case DeployType.Ftp:
                    return new FileToDeployFtp(sourcePath, targetBasePath, rule);
                case DeployType.Delete:
                    return new FileToDeployDelete(sourcePath, targetBasePath, rule);
                case DeployType.Copy:
                    return new FileToDeployCopy(sourcePath, targetBasePath, rule);
                case DeployType.Move:
                    return new FileToDeployMove(sourcePath, targetBasePath, rule);
                case DeployType.Cab:
                    return new FileToDeployCab(sourcePath, targetBasePath, rule);
                case DeployType.CopyFolder:
                    return new FileToDeployCopyFolder(sourcePath, targetBasePath, rule);
                case DeployType.DeleteFolder:
                    return new FileToDeployDeleteFolder(sourcePath, targetBasePath, rule);
                default:
                    throw new ArgumentOutOfRangeException("deployType", deployType, null);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     If this file has been added through a rule, this holds the rule reference (can be null)
        /// </summary>
        public DeployTransferRule RuleReference { get; set; }

        /// <summary>
        ///     target path computed from the deployment rules
        /// </summary>
        public string TargetBasePath { get; set; }

        /// <summary>
        ///     The path of input file that was originally compiled to trigger this move (can be equal to From)
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        ///     Need to deploy this file FROM this path
        /// </summary>
        public string From { get; set; }

        /// <summary>
        ///     Need to deploy this file TO this path
        /// </summary>
        public string To { get; set; }

        /// <summary>
        ///     true if the transfer went fine
        /// </summary>
        public bool IsOk { get; set; }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public virtual DeployType DeployType {
            get { return DeployType.Copy; }
        }

        /// <summary>
        ///     Null if no errors, otherwise it contains the description of the error that occurred for this file
        /// </summary>
        public string DeployError { get; set; }

        /// <summary>
        ///     A directory that must exist or be created for this deployment (can be null if nothing to do)
        /// </summary>
        public virtual string DirectoryThatMustExist {
            get { return Path.GetDirectoryName(To); }
        }

        /// <summary>
        ///     This is used to group the FileToDeploy during the creation of the deployment report,
        ///     use this in addition with GroupHeaderToString
        /// </summary>
        public virtual string GroupKey {
            get { return Path.GetDirectoryName(To); }
        }

        /// <summary>
        ///     Indicate whether or not this deployment can be parallelized
        /// </summary>
        public virtual bool CanBeParallelized {
            get { return true; }
        }

        /// <summary>
        ///     Indicates if this deployment is actually a deletion of a file
        /// </summary>
        public virtual bool IsDeletion {
            get { return false; }
        }

        #endregion

        #region Methods

        public virtual FileToDeploy Set(string from, string to) {
            From = from;
            To = to;
            return this;
        }

        /// <summary>
        ///     Returns a "copy" (only target path and those inputs are copied) if this object, setting properties in the meantime
        /// </summary>
        public virtual FileToDeploy Copy(string from, string to) {
            return New(DeployType, Origin, TargetBasePath, RuleReference).Set(from, to);
        }

        /// <summary>
        ///     Deploy this file
        /// </summary>
        public virtual bool DeploySelf() {
            if (IsOk)
                return false;
            IsOk = TryDeploy();
            return IsOk;
        }

        #endregion
    }

    #region FileToDeployDelete

    /// <summary>
    ///     Uses only TO
    /// </summary>
    public class FileToDeployDelete : FileToDeploy {
        #region Life and death

        public FileToDeployDelete(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        #endregion

        #region Methods

        protected override bool TryDeploy() {
            try {
                if (string.IsNullOrEmpty(To) || !File.Exists(To))
                    return true;
                File.Delete(To);
            } catch (Exception e) {
                DeployError = "Impossible de supprimer le fichier " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Delete; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        public override string GroupKey {
            get { return "Deleted"; }
        }

        /// <summary>
        ///     Indicates if this deployment is actually a deletion of a file
        /// </summary>
        public override bool IsDeletion {
            get { return true; }
        }

        #endregion
    }

    #endregion

    #region FileToDeployDeleteFolder

    public class FileToDeployDeleteFolder : FileToDeploy {
        #region Life and death

        public FileToDeployDeleteFolder(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        #endregion

        #region Methods

        protected override bool TryDeploy() {
            try {
                if (string.IsNullOrEmpty(To) || !Directory.Exists(To))
                    return true;
                Directory.Delete(To, true);
            } catch (Exception e) {
                DeployError = "Impossible de supprimer le dossier " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.DeleteFolder; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        public override string GroupKey {
            get { return "Deleted"; }
        }

        /// <summary>
        ///     Indicate whether or not this deployment can be parallelized
        /// </summary>
        public override bool CanBeParallelized {
            get { return false; }
        }

        /// <summary>
        ///     Indicates if this deployment is actually a deletion of a file
        /// </summary>
        public override bool IsDeletion {
            get { return true; }
        }

        #endregion
    }

    #endregion

    #region FileToDeployInPack

    #region FileToDeployInPack

    /// <summary>
    ///     A class for files that need to be deploy in "packs" (i.e. .zip, FTP)
    /// </summary>
    public abstract class FileToDeployInPack : FileToDeploy, IFileToDeployInPackage {
        #region Life and death

        /// <summary>
        ///     Constructor
        /// </summary>
        protected FileToDeployInPack(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        #endregion

        #region Properties

        /// <summary>
        ///     Path to the pack in which we need to include this file
        /// </summary>
        public string PackPath { get; set; }

        /// <summary>
        ///     The relative path of the file within the pack
        /// </summary>
        public string RelativePathInPack { get; set; }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return Path.GetDirectoryName(PackPath); }
        }

        /// <summary>
        ///     Path to the pack file
        /// </summary>
        public override string GroupKey {
            get { return PackPath ?? To; }
        }

        /// <summary>
        ///     Extension of the archive file
        /// </summary>
        public virtual string PackExt {
            get { return ".arc"; }
        }

        #endregion

        #region Methods

        #region Virtual

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal virtual IPackager NewArchive(Deployer deployer) {
            return null;
        }

        /// <summary>
        ///     Saves an exception in the deploy error
        /// </summary>
        public virtual void RegisterArchiveException(Exception e) {
            IsOk = false;
            DeployError = "Problème avec le pack cible " + PackPath.Quoter() + " : \"" + e.Message + "\"";
        }

        /// <summary>
        ///     Allows to check the source file before putting this fileToDeploy in a pack
        /// </summary>
        public virtual bool IfFromFileExists() {
            if (!File.Exists(From)) {
                DeployError = "Le fichier source " + From.Quoter() + " n'existe pas";
                return false;
            }
            return true;
        }

        #endregion

        public override FileToDeploy Set(string from, string to) {
            var pos = to.LastIndexOf(PackExt + @"\", StringComparison.CurrentCultureIgnoreCase);
            if (pos < 0)
                pos = to.LastIndexOf(PackExt, StringComparison.CurrentCultureIgnoreCase);
            if (pos >= 0) {
                pos += PackExt.Length;
                PackPath = to.Substring(0, pos);
                RelativePathInPack = pos + 1 < to.Length ? to.Substring(pos + 1) : "\\";
            }
            return base.Set(from, to);
        }

        #endregion
    }

    #endregion

    #region FileToDeployDeleteInProlib

    /// <summary>
    ///     Uses only FROM (to compute the PACKPATH) and the rule deployment target (to compute RELATIVEPATHINPACK)
    ///     or only TO
    /// </summary>
    public class FileToDeployDeleteInProlib : FileToDeployInPack {
        #region Life and death

        public FileToDeployDeleteInProlib(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        #endregion

        #region Properties

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.DeleteInProlib; }
        }

        public override string PackExt {
            get { return ".pl"; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        public override string GroupKey {
            get { return "Deleted .pl"; }
        }

        /// <summary>
        ///     Indicates if this deployment is actually a deletion of a file
        /// </summary>
        public override bool IsDeletion {
            get { return true; }
        }

        /// <summary>
        ///     Allows to check the source file before putting this fileToDeploy in a pack
        /// </summary>
        public override bool IfFromFileExists() {
            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IPackager NewArchive(Deployer deployer) {
            return new ProlibDelete(PackPath, deployer.ProlibPath);
        }

        public override FileToDeploy Set(string from, string to) {
            if (RuleReference != null) {
                From = from;
                PackPath = from;
                RelativePathInPack = RuleReference.DeployTarget;
                To = Path.Combine(from, RuleReference.DeployTarget);
            } else {
                base.Set(from, to);
            }
            return this;
        }

        #endregion
    }

    #endregion

    #region FileToDeployCab

    public class FileToDeployCab : FileToDeployInPack {
        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Cab; }
        }

        public override string PackExt {
            get { return ".cab"; }
        }

        public FileToDeployCab(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IPackager NewArchive(Deployer deployer) {
            return new CabPackager(PackPath);
        }
    }

    #endregion

    #region FileToDeployProlib

    public class FileToDeployProlib : FileToDeployInPack {
        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Prolib; }
        }

        public override string PackExt {
            get { return ".pl"; }
        }

        public FileToDeployProlib(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IPackager NewArchive(Deployer deployer) {
            return new ProlibPackager(PackPath, deployer.ProlibPath);
        }
    }

    #endregion

    #region FileToDeployZip

    public class FileToDeployZip : FileToDeployInPack {
        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Zip; }
        }

        public override string PackExt {
            get { return ".zip"; }
        }

        public FileToDeployZip(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IPackager NewArchive(Deployer deployer) {
            return new ZipPackager(PackPath);
        }
    }

    #endregion

    #region FileToDeployFtp

    public class FileToDeployFtp : FileToDeployInPack {
        private string _host;
        private string _passWord;
        private int _port;
        private string _userName;

        #region Life and death

        /// <summary>
        ///     Constructor
        /// </summary>
        public FileToDeployFtp(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        #endregion

        #region Properties

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Ftp; }
        }

        /// <summary>
        ///     Path to the pack file
        /// </summary>
        public override string GroupKey {
            get { return PackPath ?? To; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        #endregion

        #region Methods

        public override FileToDeploy Set(string from, string to) {
            // parse our uri
            var regex = new Regex(@"^(ftps?:\/\/([^:\/@]*)?(:[^:\/@]*)?(@[^:\/@]*)?(:[^:\/@]*)?)(\/.*)$");
            var match = regex.Match(to.Replace("\\", "/"));
            if (match.Success) {
                PackPath = match.Groups[1].Value;
                RelativePathInPack = match.Groups[6].Value;
                if (!string.IsNullOrEmpty(match.Groups[4].Value)) {
                    _userName = match.Groups[2].Value;
                    _passWord = match.Groups[3].Value.Trim(':');
                    _host = match.Groups[4].Value.Trim('@');
                    if (!int.TryParse(match.Groups[5].Value.Trim(':'), out _port))
                        _port = -1;
                } else {
                    _host = match.Groups[2].Value;
                    if (!int.TryParse(match.Groups[3].Value.Trim(':'), out _port))
                        _port = -1;
                }
            }
            return base.Set(from, to);
        }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IPackager NewArchive(Deployer deployer) {
            return new FtpPackager(_host, _port, _userName, _passWord, PackPath);
        }

        /// <summary>
        ///     Saves an exception in the deploy error
        /// </summary>
        public override void RegisterArchiveException(Exception e) {
            IsOk = false;
            DeployError = "Problème avec le serveur FTP " + PackPath.Quoter() + " : \"" + e.Message + "\"";
        }

        #endregion
    }

    #endregion

    #endregion

    #region FileToDeployCopyFolder

    public class FileToDeployCopyFolder : FileToDeploy {
        #region Life and death

        public FileToDeployCopyFolder(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        #endregion

        #region Methods

        protected override bool TryDeploy() {
            try {
                if (!Directory.Exists(From)) {
                    DeployError = "Le dossier source " + From.Quoter() + " n'existe pas";
                    return false;
                }
                // make sure that both From and To finish with \
                From = Path.GetFullPath(From);
                To = Path.GetFullPath(To);
                // create all of the directories
                foreach (var dirPath in Directory.EnumerateDirectories(From, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(dirPath.Replace(From, To));
                // copy all the files & replaces any files with the same name
                foreach (var newPath in Directory.EnumerateFiles(From, "*.*", SearchOption.AllDirectories)) File.Copy(newPath, newPath.Replace(From, To), true);
            } catch (Exception e) {
                DeployError = "Impossible de copier le dossier " + From.Quoter() + " vers " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.CopyFolder; }
        }

        /// <summary>
        ///     This is used to group the FileToDeploy during the creation of the deployment report,
        ///     use this in addition with GroupHeaderToString
        /// </summary>
        public override string GroupKey {
            get { return To; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        #endregion
    }

    #endregion

    #region FileToDeployCopy

    public class FileToDeployCopy : FileToDeploy {
        /// <summary>
        ///     This can be set to true for a file deployed during step 0 (compilation), if the last
        ///     deployment is a Copy, we make it a Move because this allows us to directly compile were
        ///     we need to finally move it instead of compiling then copying...
        /// </summary>
        public bool FinalDeploy { get; set; }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return FinalDeploy ? DeployType.Move : DeployType.Copy; }
        }

        public FileToDeployCopy(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        protected override bool TryDeploy() {
            try {
                if (From.Equals(To))
                    return true;
                if (!File.Exists(From)) {
                    DeployError = "Le fichier source " + From.Quoter() + " n'existe pas";
                    return false;
                }
                File.Copy(From, To, true);
            } catch (Exception e) {
                DeployError = "Impossible de copier " + From.Quoter() + " vers  " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }
    }

    #endregion

    #region FileToDeployMove

    public class FileToDeployMove : FileToDeploy {
        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Move; }
        }

        public FileToDeployMove(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        protected override bool TryDeploy() {
            try {
                if (From.Equals(To))
                    return true;
                if (!File.Exists(From)) {
                    DeployError = "Le fichier source " + From.Quoter() + " n'existe pas";
                    return false;
                }
                File.Delete(To);
                File.Move(From, To);
            } catch (Exception e) {
                DeployError = "Impossible de déplacer " + From.Quoter() + " vers  " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }
    }

    #endregion
}