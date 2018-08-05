﻿// ========================================================================
// Copyright (c) 2017 - Julien Caillon (julien.caillon@gmail.com)
// This file (DeploymentHandlerDifferential.cs) is part of csdeployer.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Oetools.Packager.Core.Config;
using Oetools.Packager.Core.Exceptions;
using Oetools.Packager.Core2.Execution;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Packager.Core {

    /// <summary>
    ///     This type of deployment handles the difference between the current source directory state and an older state
    /// </summary>
    public class DeploymentHandlerDifferential : DeploymentHandler {
        /// <summary>
        ///     Constructor
        /// </summary>
        public DeploymentHandlerDifferential(ConfigDeploymentDifferential conf) : base(conf) {
            Conf = conf;

            // set previously deployed files + previous source files
            var lastDeployedFiles = conf.LastDeployedFiles; 
            if (lastDeployedFiles != null) {
                _previousDeployedFiles = ConvertToAbsolutePath(lastDeployedFiles.Where(deployed => deployed.Action != DeploymentAction.Deleted).ToNonNullList());
                _prevSourceFiles.Clear();
                // add all previously deployed
                foreach (var file in _previousDeployedFiles
                    .Where(file => !_prevSourceFiles.ContainsKey(file.SourcePath))) {
                    _prevSourceFiles.Add(file.SourcePath, file);
                }
                // all also all the required files
                foreach (var file in _previousDeployedFiles
                    .Where(file => file is FileDeployedCompiled)
                    .Cast<FileDeployedCompiled>()
                    .SelectMany(file => file.RequiredFiles)
                    .Where(file => file != null && !_prevSourceFiles.ContainsKey(file.SourcePath))) {
                    _prevSourceFiles.Add(file.SourcePath, file);
                }
            }
        }

        public new ConfigDeploymentDifferential Conf { get; protected set; }

        /// <summary>
        ///     max step number composing this deployment, we ensure it to be at least 2
        /// </summary>
        public override int MaxStep {
            get { return Math.Max(_maxStep, 2); }
        }

        /// <summary>
        ///     We add an operation, that will be : listing all the source files
        /// </summary>
        public override int TotalNumberOfOperations {
            get { return base.TotalNumberOfOperations + 1; }
        }

        /// <summary>
        ///     add the listing operation
        /// </summary>
        public override float OverallProgressionPercentage {
            get { return base.OverallProgressionPercentage + _listingPercentage; }
        }

        /// <summary>
        ///     Returns the name of the current step
        /// </summary>
        public override DeploymentStep CurrentOperationName {
            get { return _isListing ? DeploymentStep.Listing : base.CurrentOperationName; }
        }

        /// <summary>
        ///     Returns the progression for the current step
        /// </summary>
        public override float CurrentOperationPercentage {
            get { return _isListing ? _listingPercentage : base.CurrentOperationPercentage; }
        }

        /// <summary>
        ///     When true, we activate the log just before compiling with FileId active + we generate a file that list referenced
        ///     table in the .r
        /// </summary>
        public override bool IsAnalysisMode {
            get { return true; }
        }

        /// <summary>
        ///     This returns a serializable list of files that were compiled/deployed during this deployment
        /// </summary>
        public List<FileDeployed> DeployedFilesOutput {
            get {
                if (_deployedFilesOutput == null) {
                    var list = new Dictionary<string, FileDeployed>(StringComparer.CurrentCultureIgnoreCase);

                    // we will save data on each file that was deployed from the source directory
                    foreach (var kpv in _filesToDeployPerStep.Where(kpv => kpv.Key <= 2)) {
                        var step = kpv.Key;
                        foreach (var fileDeployed in kpv.Value) {
                            if (step == 2 && !fileDeployed.IsDeletion) continue;

                            FileDeployed listFile;
                            if (!list.ContainsKey(fileDeployed.Origin)) {
                                var compiledFile = step > 0 ? null : _proCompilation.ListFilesToCompile.Find(compile => compile.CompiledPath.Equals(fileDeployed.Origin));
                                if (compiledFile != null) {
                                    listFile = new FileDeployedCompiled();
                                    var newCompiledFile = (FileDeployedCompiled) listFile;
                                    // we only keep required files located in the source directory
                                    newCompiledFile.RequiredFiles = compiledFile.RequiredFiles == null ? null : compiledFile.RequiredFiles.Where(path => !string.IsNullOrEmpty(path) && path.StartsWith(Env.SourceDirectory)).Select(GetSourceFileBaseInfo).Where(info => info != null).ToList();
                                    // we only keep user's tables
                                    newCompiledFile.RequiredTables = compiledFile.RequiredTables.Where(table => !table.QualifiedTableName.Contains("._")).ToList();
                                } else {
                                    listFile = new FileDeployed();
                                }

                                listFile.SourcePath = fileDeployed.Origin;
                                var fileInf = GetSourceFileBaseInfo(fileDeployed.Origin);
                                if (fileInf != null) {
                                    listFile.Size = fileInf.Size;
                                    listFile.LastWriteTime = fileInf.LastWriteTime;
                                    listFile.Md5 = fileInf.Md5;
                                }
                                list.Add(fileDeployed.Origin, listFile);
                            } else {
                                listFile = list[fileDeployed.Origin];
                            }

                            if (fileDeployed.IsDeletion) {
                                listFile.Action = DeploymentAction.Deleted;
                            } else {
                                if (_prevSourceFiles.ContainsKey(listFile.SourcePath)) listFile.Action = DeploymentAction.Replaced;
                                else listFile.Action = DeploymentAction.Added;
                            }

                            if (listFile.Targets == null) listFile.Targets = new List<DeploymentTarget>();

                            // we add a new deployment target for this file (only if it is deployed in the target dir)
                            var fileToDeployInPack = fileDeployed as FileToDeployInPack;
                            listFile.Targets.Add(new DeploymentTarget {
                                TargetPath = fileDeployed.To,
                                TargetPackPath = fileToDeployInPack == null ? null : fileToDeployInPack.ArchivePath,
                                TargetPathInPack = fileToDeployInPack == null ? null : fileToDeployInPack.RelativePathInArchive,
                                DeployType = fileDeployed.DeployType
                            });
                        }
                    }

                    // for each file previously deployed that has not been deployed again, we also keep the data
                    foreach (var file in _sourceFilesUpToDate
                        .Where(path => _prevSourceFiles.ContainsKey(path) && !list.ContainsKey(path))
                        .Select(path => _prevSourceFiles[path])
                        .Where(file => file is FileDeployed)
                        .Cast<FileDeployed>()) {
                        list.Add(file.SourcePath, file);
                        file.Action = DeploymentAction.Existing;
                        // update the md5 value?
                        if (Conf.ComputeMd5 && (file.Md5 == null || file.Md5.Length == 0)) {
                            file.Md5 = GetSourceFileBaseInfo(file.SourcePath).Md5;
                        }
                    }

                    _deployedFilesOutput = ConvertToRelativePath(list.Values.ToList());
                }

                return _deployedFilesOutput;
            }
        }

        public Dictionary<string, FileSourceInfo> SourceFiles {
            get { return _sourceFiles; }
        }

        public HashSet<string> SourceFilesUpToDate {
            get { return _sourceFilesUpToDate; }
        }

        public HashSet<string> SourceFilesNew {
            get { return _sourceFilesNew; }
        }

        public HashSet<string> SourceFilesMissing {
            get { return _sourceFilesMissing; }
        }

        /// <summary>
        ///     List of all the files previously in the source dir
        /// </summary>
        protected List<FileDeployed> _previousDeployedFiles;

        /// <summary>
        ///     data for each source file that existed (and were used) in the previous deployment
        /// </summary>
        private Dictionary<string, FileSourceInfo> _prevSourceFiles = new Dictionary<string, FileSourceInfo>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        ///     Keep data for each current source file
        /// </summary>
        private Dictionary<string, FileSourceInfo> _sourceFiles = new Dictionary<string, FileSourceInfo>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        ///     A list of all the files that were already deployed last time and that didn't change since
        /// </summary>
        private HashSet<string> _sourceFilesUpToDate = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        ///     A list of all the new files (that either didn't exist in the previous deployment or that were modified)
        /// </summary>
        private HashSet<string> _sourceFilesNew = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        ///     A list of all the files that are missing compared to the previous deployment
        /// </summary>
        private HashSet<string> _sourceFilesMissing = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        ///     Files deployed during this deployment
        /// </summary>
        private List<FileDeployed> _deployedFilesOutput;

        private float _listingPercentage;

        private bool _isListing;

        /// <summary>
        ///     Do stuff before starting the treatment
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DeploymentException"></exception>
        protected override void BeforeStarting() {
            // creates a list of all the files in the source directory, gather info on each file
            ListAllFilesInSourceDir();
        }

        /// <summary>
        ///     Make the list of all the files that need to be (re)compiled
        /// </summary>
        /// <returns></returns>
        protected override List<FileToCompile> GetFilesToCompileInStepZero() {
            // Full deployment? 
            if (Conf.ForceFullDeploy || _previousDeployedFiles == null)
                return Env.Deployer.GetFilteredList(_sourceFiles.Keys, 0)
                    .Where(path => path.TestAgainstListOfPatterns(Conf.FilesPatternCompilable))
                    .Select(path => new FileToCompile(path))
                    .ToNonNullList();

            // list of files that need to be recompiled simply because the source changed
            var filesToCompile = new HashSet<string>(_sourceFilesNew, StringComparer.CurrentCultureIgnoreCase);

            // list the files that need to be recompiled because of table CRC change
            // and the files that need to be recompiled because one of the file needed to compile it has changed
            FilesToCompileBecauseOfTableCrcChangesOrDependencesModification(ref filesToCompile);

            return Env.Deployer.GetFilteredList(filesToCompile, 0)
                .Where(path => path.TestAgainstListOfPatterns(Conf.FilesPatternCompilable))
                .Select(s => new FileToCompile(s, GetSourceFileBaseInfo(s).Size))
                .ToNonNullList();
        }

        /// <summary>
        ///     List all the files that should be deployed from the source directory
        /// </summary>
        protected override List<FileToDeploy> GetFilesToDeployInStepOne() {
            // Full deployment? 
            if (Conf.ForceFullDeploy || _previousDeployedFiles == null)
                return Env.Deployer.GetFilteredList(_sourceFiles.Keys, 1)
                    .SelectMany(file => Env.Deployer.GetTransfersNeededForFile(file, 1))
                    .ToNonNullList();

            // list of files that need to be recompiled simply because the source changed
            return Env.Deployer.GetFilteredList(_sourceFilesNew, 1)
                .SelectMany(file => Env.Deployer.GetTransfersNeededForFile(file, 1))
                .ToNonNullList();
        }

        protected override List<FileToDeploy> GetFilesToDeployInStepTwoAndMore(int currentStep) {
            var list = base.GetFilesToDeployInStepTwoAndMore(currentStep);

            if (currentStep == 2)
                foreach (var fileDeployed in _sourceFilesMissing
                    .Where(path => _prevSourceFiles.ContainsKey(path))
                    .Select(path => _prevSourceFiles[path])
                    .Where(file => file is FileDeployed)
                    .Cast<FileDeployed>())
                foreach (var target in fileDeployed.Targets) {
                    // we can't delete certain files (for instance, files deployed in a .cab)
                    var deletionType = DeployTransferRule.GetDeletetionType(target.DeployType);
                    if (deletionType == DeployType.None)
                        continue;

                    // don't delete files we just deployed! (happens when you moved a file in the source directory)
                    if (_filesToDeployPerStep.SelectMany(pair => pair.Value).ToNonNullList().Exists(deploy => deploy.To.EqualsCi(target.TargetPath)))
                        continue;

                    list.Add(FileToDeploy.New(deletionType, fileDeployed.SourcePath, Path.GetDirectoryName(target.TargetPath), null)
                        .Set(fileDeployed.SourcePath, target.TargetPath));
                }

            return list;
        }

        /// <summary>
        ///     Creates a list of all the files in the source directory, gather info on each file
        /// </summary>
        protected void ListAllFilesInSourceDir() {
            try {
                _isListing = true;

                // get the list of files in the source dir with their basic info
                var step = 0.5f;
                foreach (var file in Utils.EnumerateFiles(Env.SourceDirectory, "*", Conf.ExploreRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
                    _cancelSource.Token.ThrowIfCancellationRequested();
                    GetSourceFileBaseInfo(file);
                    if ((Conf.ComputeMd5 ? 30 : 90) - _listingPercentage < 10 * step) step /= 10;
                    _listingPercentage += step;
                }

                // compute MD5 for each file (that's the step that takes the most time)
                // TODO : we compute the MD5 of each but we only need to compute the MD5 of files that do not have the same size/modification at this point.
                // TODO : we will compute the MD5 when we output the list of deployed files...
                if (Conf.ComputeMd5) {
                    var step1 = (90 - _listingPercentage) / _sourceFiles.Count;
                    var parallelOptions = new ParallelOptions {CancellationToken = _cancelSource.Token};
                    Parallel.ForEach(_sourceFiles, parallelOptions, pair => {
                        parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                        try {
                            using (var md5 = MD5.Create()) {
                                using (var stream = File.OpenRead(pair.Value.SourcePath)) {
                                    pair.Value.Md5 = md5.ComputeHash(stream);
                                }
                            }
                        } catch (Exception e) {
                            AddHandledExceptions(e, "Impossible d'obtenir le MD5 du fichier " + pair.Value.SourcePath.Quoter());
                        }
                        _listingPercentage += step1;
                    });
                }

                // now list all the files that are new (either they didn't exist in the previous deployment or they changed since)
                // TODO : parrellize this and compute MD5 only when needed
                step = (95 - _listingPercentage) / _sourceFiles.Count;
                foreach (var file in _sourceFiles) {
                    _listingPercentage += step;
                    _cancelSource.Token.ThrowIfCancellationRequested();
                    var source = file.Key;
                    // the file existed before
                    if (_prevSourceFiles.ContainsKey(source)) {
                        var prevFile = _prevSourceFiles[source];

                        // test if it has changed
                        if (prevFile.Size.Equals(file.Value.Size))
                            if (prevFile.LastWriteTime.Equals(file.Value.LastWriteTime) ||
                                Conf.ComputeMd5 && prevFile.Md5 != null && file.Value.Md5 != null && prevFile.Md5.SequenceEqual(file.Value.Md5)) {
                                if (!_sourceFilesUpToDate.Contains(file.Key))
                                    _sourceFilesUpToDate.Add(file.Key);
                                continue;
                            }
                    }
                    if (!_sourceFilesNew.Contains(file.Key))
                        _sourceFilesNew.Add(file.Key);
                }

                // list all the files that are missing from the previous deployment
                step = (100 - _listingPercentage) / _sourceFiles.Count;
                foreach (var prevFilePath in _prevSourceFiles.Keys) {
                    if (!_sourceFiles.ContainsKey(prevFilePath) && !_sourceFilesMissing.Contains(prevFilePath)) _sourceFilesMissing.Add(prevFilePath);
                    _listingPercentage += step;
                }

                _listingPercentage = 100;
                _isListing = false;
            } catch (OperationCanceledException) {
                // we expect this exception if the task has been canceled
            } catch (Exception e) {
                throw new DeploymentException("An error has occurred while listing the files in the source directory", e);
            }
        }

        /// <summary>
        ///     list the files that need to be recompiled because of table CRC change OR
        ///     because one of the file needed to compile it has changed
        ///     We only take into account include files, we don't consider that a file that has dependences (i.e. a *.p needs a
        ///     *.i)
        ///     can be a dependence of another file
        /// </summary>
        private void FilesToCompileBecauseOfTableCrcChangesOrDependencesModification(ref HashSet<string> filesToCompile) {
            // we get a list of the current DB.TABLE + the CRC for each
            var currentTables = new List<TableCrc>();

            if (!string.IsNullOrEmpty(Env.ConnectionString)) {
                var exec = new ProExecutionTableCrc(new DatabaseExtractionOptions(), Env) {
                    NeedDatabaseConnection = true
                };
                exec.Start();
                exec.WaitForProcessExit();
                currentTables = exec.GetTableCrc();
                if (currentTables == null) return;
            }

            // for each previously compiled file that required a table
            foreach (var prevCompiledFile in _previousDeployedFiles
                .Where(file => file is FileDeployedCompiled)
                .Cast<FileDeployedCompiled>()) {
                // if the file is not part of the sources anymore, no need to recompile obviously
                if (!_sourceFiles.ContainsKey(prevCompiledFile.SourcePath)) continue;
                // we already added this file to be recompiled, continue
                if (filesToCompile.Contains(prevCompiledFile.SourcePath)) continue;
                // for each required table of said file
                if (prevCompiledFile.RequiredTables != null)
                    foreach (var tableRequired in prevCompiledFile.RequiredTables) {
                        var currentTable = currentTables.Find(table => table.QualifiedTableName.EqualsCi(tableRequired.QualifiedTableName));

                        // the file uses a now unknown table or uses a table for which the CRC changed?
                        if (currentTable == null || !currentTable.Crc.Equals(tableRequired.Crc)) filesToCompile.Add(prevCompiledFile.SourcePath);
                    }
                // for each required file
                if (prevCompiledFile.RequiredFiles != null)
                    foreach (var requiredFile in prevCompiledFile.RequiredFiles.Where(file => file != null)) // one of the required file has changed
                        if (_sourceFilesNew.Contains(requiredFile.SourcePath)) filesToCompile.Add(prevCompiledFile.SourcePath);
            }
        }

        /// <summary>
        ///     Returns the basic info for a given file path (in the source dir)
        /// </summary>
        private FileSourceInfo GetSourceFileBaseInfo(string sourcePath) {
            try {
                if (_sourceFiles.ContainsKey(sourcePath)) {
                    return _sourceFiles[sourcePath];
                }
                if (!File.Exists(sourcePath)) {
                    return null;
                }
                var fileInfo = new FileInfo(sourcePath);
                var newInfo = new FileSourceInfo {
                    SourcePath = sourcePath,
                    Size = fileInfo.Length,
                    LastWriteTime = fileInfo.LastWriteTime
                };
                _sourceFiles.Add(sourcePath, newInfo);
            } catch (Exception e) {
                AddHandledExceptions(e, "Failed to get file info for : " + sourcePath.Quoter());
            }
            return null;
        }

        /// <summary>
        ///     Convert each path of each object to a relative path
        /// </summary>
        private List<FileDeployed> ConvertToRelativePath(List<FileDeployed> list) {
            foreach (var file in list)
                try {
                    file.SourcePath = file.SourcePath.Replace(Env.SourceDirectory.CorrectDirPath(), "");
                    if (file.Targets != null)
                        foreach (var target in file.Targets) {
                            target.TargetPath = target.TargetPath.Replace(Env.TargetDirectory.CorrectDirPath(), "");
                            if (!string.IsNullOrEmpty(target.TargetPackPath))
                                target.TargetPackPath = target.TargetPackPath.Replace(Env.TargetDirectory.CorrectDirPath(), "");
                        }
                    var compFile = file as FileDeployedCompiled;
                    if (compFile != null && compFile.RequiredFiles != null) foreach (var fileRequired in compFile.RequiredFiles) fileRequired.SourcePath = fileRequired.SourcePath.Replace(Env.SourceDirectory.CorrectDirPath(), "");
                } catch (Exception e) {
                    AddHandledExceptions(e);
                }
            return list;
        }

        /// <summary>
        ///     Convert each path of each object to an absolute path
        /// </summary>
        private List<FileDeployed> ConvertToAbsolutePath(List<FileDeployed> list) {
            // we make each path absolute
            foreach (var file in list)
                try {
                    file.SourcePath = Path.Combine(Env.SourceDirectory, file.SourcePath);
                    if (file.Targets != null)
                        foreach (var target in file.Targets) {
                            target.TargetPath = Path.Combine(Env.TargetDirectory, target.TargetPath);
                            if (!string.IsNullOrEmpty(target.TargetPackPath))
                                target.TargetPackPath = Path.Combine(Env.TargetDirectory, target.TargetPackPath);
                        }
                    var compFile = file as FileDeployedCompiled;
                    if (compFile != null && compFile.RequiredFiles != null) foreach (var fileRequired in compFile.RequiredFiles) fileRequired.SourcePath = Path.Combine(Env.SourceDirectory, fileRequired.SourcePath);
                } catch (Exception e) {
                    AddHandledExceptions(e);
                }
            return list;
        }
    }
}