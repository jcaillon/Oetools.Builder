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
        
        internal static IEnumerable<OeFile> GetSourceFilesToRebuildBecauseTheyHaveNewTargets(List<OeFile> allExistingSourceFilesWithSetTargets, List<OeFileBuilt> previousFilesBuilt) {
            foreach (var newFile in allExistingSourceFilesWithSetTargets.Where(file => file.State == OeFileState.Unchanged)) {
                var previousFile = previousFilesBuilt.First(prevFile => prevFile.SourceFilePath.Equals(newFile.SourceFilePath, StringComparison.CurrentCultureIgnoreCase));
                var previouslyCreatedTargets = previousFile.Targets.ToNonNullList().Where(target => !target.IsDeletionMode()).Select(t => t.GetTargetPath()).ToList();
                foreach (var targetPath in newFile.GetAllTargets().Select(t => t.GetTargetPath())) {
                    if (!previouslyCreatedTargets.Exists(prevTarget => prevTarget.Equals(targetPath, StringComparison.CurrentCultureIgnoreCase))) {
                        yield return newFile;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a raw list of files that need to be rebuilt because one of their dependencies (source file, include) has been modified (modified/deleted)
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list
        /// </summary>
        /// <param name="filesModified"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<OeFile> GetSourceFilesToRebuildBecauseOfDependenciesModification(List<OeFile> filesModified, List<OeFileBuiltCompiled> previousFilesBuilt) {
            for (int i = 0; i < filesModified.Count; i++) {
                bool firstAdd = true;
                foreach (var result in previousFilesBuilt.Where(prevf => prevf.RequiredFiles != null && 
                    prevf.RequiredFiles.Any(prevFile => filesModified[i].SourceFilePath.Equals(prevFile, StringComparison.CurrentCultureIgnoreCase)))) {
                    if (firstAdd) {
                        filesModified.Add(result);
                    }
                    firstAdd = false;
                    yield return result.GetDeepCopy();
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
        internal static IEnumerable<OeFile> GetSourceFilesToRebuildBecauseOfTableCrcChanges(UoeExecutionEnv env, List<OeFileBuiltCompiled> previousFilesBuilt) {
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
                    yield return previousFile.GetDeepCopy();
                }
            }
        }
        
        /// <summary>
        /// Get a list of previously built files that are now deleted, their targets should be removed
        /// </summary>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<OeFileBuilt> GetBuiltFilesDeletedSincePreviousBuild(List<OeFileBuilt> previousFilesBuilt) {
            foreach (var previousFile in previousFilesBuilt.Where(f => f.State != OeFileState.Deleted)) {
                if (!File.Exists(previousFile.SourceFilePath) && previousFile.Targets != null && previousFile.Targets.Count > 0) {
                    var previousFileCopy = (OeFileBuilt) Utils.DeepCopyPublicProperties(previousFile, previousFile.GetType());
                    previousFileCopy.Targets.ForEach(target => target.SetDeletionMode(true));
                    previousFileCopy.State = OeFileState.Deleted;
                    yield return previousFileCopy;
                }
            }
        }

        /// <summary>
        /// Get a list of previously built files, still existing, but with targets that no longer exist and should be removed
        /// </summary>
        /// <param name="allExistingSourceFilesWithSetTargets"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<OeFileBuilt> GetBuiltFilesWithOldTargetsToRemove(List<OeFile> allExistingSourceFilesWithSetTargets, List<OeFileBuilt> previousFilesBuilt) {
            var finalFileTargets = new List<OeTarget>();
            foreach (var newFile in allExistingSourceFilesWithSetTargets.Where(file => file.State == OeFileState.Unchanged || file.State == OeFileState.Modified)) {
                var previousFile = previousFilesBuilt.FirstOrDefault(file => file.State != OeFileState.Deleted && file.SourceFilePath.Equals(newFile.SourceFilePath, StringComparison.CurrentCultureIgnoreCase));
                if (previousFile == null) {
                    throw new Exception($"Could not find the history of a now unchanged or modified file, something is wrong! File : {newFile}");
                }
                finalFileTargets.Clear();
                bool isFileWithTargetsToDelete = false;
                var newCreateTargets = newFile.GetAllTargets().Select(t => t.GetTargetPath()).ToList();
                foreach (var previousTarget in previousFile.Targets.ToNonNullList().Where(target => !target.IsDeletionMode())) {
                    var previousTargetPath = previousTarget.GetTargetPath();
                    if (!newCreateTargets.Exists(target => target.Equals(previousTargetPath, StringComparison.CurrentCultureIgnoreCase))) {
                        // the old target doesn't exist anymore, add it in deletion mode this time
                        isFileWithTargetsToDelete = true;
                        finalFileTargets.Add(previousTarget);
                    }
                }
                if (isFileWithTargetsToDelete) {
                    var originalPreviousFileTargets = previousFile.Targets.ToList();
                    previousFile.Targets = finalFileTargets;
                    var previousFileCopy = (OeFileBuilt) Utils.DeepCopyPublicProperties(previousFile, previousFile.GetType());
                    previousFile.Targets = originalPreviousFileTargets;
                    previousFileCopy.Targets.ForEach(target => target.SetDeletionMode(true));
                    switch (newFile.State) {
                        case OeFileState.Unchanged:
                            // add the unchanged targets
                            previousFileCopy.Targets.AddRange(newFile.GetAllTargets());
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