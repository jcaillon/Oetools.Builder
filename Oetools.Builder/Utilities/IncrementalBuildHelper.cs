#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IncrementalBuildHelper.cs) is part of Oetools.Builder.
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
using System.Linq;
using System.Runtime.CompilerServices;
using Oetools.Builder.History;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

[assembly: InternalsVisibleTo("Oetools.Builder.Test")]

namespace Oetools.Builder.Utilities {
    
    internal static class IncrementalBuildHelper {

        /// <summary>
        /// Returns a raw list of files that need to be rebuilt because one of their dependencies (source file, include) has been modified (modified/deleted)
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list
        /// </summary>
        /// <param name="pathsModified"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFile> GetSourceFilesToRebuildBecauseOfDependenciesModification(PathList<IOeFile> pathsModified, List<OeFileBuiltCompiled> previousFilesBuilt) {
            var filesModifiedList = pathsModified.ToList();
            for (int i = 0; i < filesModifiedList.Count; i++) {
                var fileModified = filesModifiedList[i];
                bool firstAdd = true;
                foreach (var result in previousFilesBuilt
                    .Where(prevf => prevf.RequiredFiles != null && prevf.RequiredFiles.Any(prevFile => fileModified.Path.PathEquals(prevFile)))) 
                {
                    if (firstAdd) {
                        filesModifiedList.Add(result);
                    }
                    firstAdd = false;
                    yield return new OeFile(result);
                }
            }
        }
        
        /// <summary>
        /// Returns a raw list of files that need to be rebuilt because one of their database references (table or sequence) has been modified (modified/deleted)
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list
        /// </summary>
        /// <param name="env"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFile> GetSourceFilesToRebuildBecauseOfTableCrcChanges(UoeExecutionEnv env, IEnumerable<OeFileBuiltCompiled> previousFilesBuilt) {
            var sequences = env.Sequences;
            var tables = env.TablesCrc;
            
            // add all previous that required a database reference that has now changed
            foreach (var previousFile in previousFilesBuilt) {
                var allReferencesOk = previousFile.RequiredDatabaseReferences?.All(dRef => {
                    switch (dRef) {
                        case OeDatabaseReferenceSequence sequence:
                            return sequences.Contains(sequence.QualifiedName);
                        case OeDatabaseReferenceTable table:
                            return tables.ContainsKey(table.QualifiedName) && tables[table.QualifiedName].EqualsCi(table.Crc);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }) ?? true;
                if (!allReferencesOk) {
                    yield return new OeFile(previousFile);
                }
            }
        }
        
        /// <summary>
        /// Get a list of previously built files that are now deleted, their targets should be removed
        /// </summary>
        /// <param name="previousPathsBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFileBuilt> GetBuiltFilesDeletedSincePreviousBuild(PathList<IOeFileBuilt> previousPathsBuilt) {
            foreach (var previousFile in previousPathsBuilt.Where(f => f.State != OeFileState.Deleted)) {
                if (!File.Exists(previousFile.Path) && previousFile.Targets != null && previousFile.Targets.Count > 0) {
                    var previousFileCopy = new OeFileBuilt(previousFile);
                    previousFileCopy.Targets.ForEach(target => target.SetDeletionMode(true));
                    previousFileCopy.State = OeFileState.Deleted;
                    yield return previousFileCopy;
                }
            }
        }
        
        /// <summary>
        /// List of the source file that are otherwise unchanged by need to be rebuild because they have new targets not present in the last build
        /// </summary>
        /// <param name="allUnchangedSourcePathsWithSetTargets"></param>
        /// <param name="previousPathsBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFileToBuild> GetSourceFilesToRebuildBecauseTheyHaveNewTargets(PathList<IOeFileToBuild> allUnchangedSourcePathsWithSetTargets, PathList<IOeFileBuilt> previousPathsBuilt) {
            foreach (var newFile in allUnchangedSourcePathsWithSetTargets.Where(file => file.State == OeFileState.Unchanged)) {
                var previousFile = previousPathsBuilt[newFile.Path];
                var previouslyCreatedTargets = previousFile.Targets.ToNonNullList().Where(target => !target.IsDeletionMode()).Select(t => t.GetTargetPath()).ToList();
                foreach (var targetPath in newFile.TargetsToBuild.Select(t => t.GetTargetPath())) {
                    if (!previouslyCreatedTargets.Exists(prevTarget => prevTarget.PathEquals(targetPath))) {
                        yield return newFile;
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Get a list of previously built files, still existing, but with targets that no longer exist and should be removed
        /// </summary>
        /// <param name="allUnchangedOrModifiedSourcePathsWithSetTargets"></param>
        /// <param name="previousPathsBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFileBuilt> GetBuiltFilesWithOldTargetsToRemove(PathList<IOeFileToBuild> allUnchangedOrModifiedSourcePathsWithSetTargets, PathList<IOeFileBuilt> previousPathsBuilt) {
            var finalFileTargets = new List<AOeTarget>();
            foreach (var newFile in allUnchangedOrModifiedSourcePathsWithSetTargets.Where(file => file.State == OeFileState.Unchanged || file.State == OeFileState.Modified)) {
                var previousFile = previousPathsBuilt[newFile.Path];
                if (previousFile == null) {
                    throw new Exception($"Could not find the history of a now unchanged or modified file, something is wrong! File : {newFile}");
                }
                if (previousFile.State == OeFileState.Deleted) {
                    continue;
                }
                finalFileTargets.Clear();
                bool isFileWithTargetsToDelete = false;
                var newCreateTargets = newFile.TargetsToBuild.Select(t => t.GetTargetPath()).ToList();
                foreach (var previousTarget in previousFile.Targets.ToNonNullList().Where(target => !target.IsDeletionMode())) {
                    var previousTargetPath = previousTarget.GetTargetPath();
                    if (!newCreateTargets.Exists(target => target.PathEquals(previousTargetPath))) {
                        // the old target doesn't exist anymore, add it in deletion mode this time
                        isFileWithTargetsToDelete = true;
                        finalFileTargets.Add(previousTarget);
                    }
                }
                if (isFileWithTargetsToDelete) {
                    var originalPreviousFileTargets = previousFile.Targets.ToList();
                    previousFile.Targets = finalFileTargets;
                    var previousFileCopy = new OeFileBuilt(previousFile);
                    previousFile.Targets = originalPreviousFileTargets;
                    previousFileCopy.Targets.ForEach(target => target.SetDeletionMode(true));
                    switch (newFile.State) {
                        case OeFileState.Unchanged:
                            // add the unchanged targets
                            previousFileCopy.Targets.AddRange(newFile.TargetsToBuild);
                            previousFileCopy.State = OeFileState.Unchanged;
                            break;
                        case OeFileState.Modified:
                            previousFileCopy.State = OeFileState.Modified;
                            // no need to add the unchanged targets because the file has been modified so those targets will be rebuild anyway
                            break;
                        default:
                            throw new Exception($"Unexpected file state for file : {newFile}");
                    }
                    yield return previousFileCopy;
                }
            }
        }
    }
}