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
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list.
        /// </summary>
        /// <param name="pathsModified"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFile> GetSourceFilesToRebuildBecauseOfDependenciesModification(PathList<IOeFile> pathsModified, PathList<IOeFileBuilt> previousFilesBuilt) {
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
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list.
        /// </summary>
        /// <param name="env"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFile> GetSourceFilesToRebuildBecauseOfTableCrcChanges(UoeExecutionEnv env, IEnumerable<IOeFileBuilt> previousFilesBuilt) {
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
        /// Returns a raw list of files that need to be rebuilt they did not correctly compile last build.
        /// </summary>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFile> GetSourceFilesToRebuildBecauseOfCompilationErrors(IEnumerable<IOeFileBuilt> previousFilesBuilt) {
            foreach (var previousFile in previousFilesBuilt) {
                if (previousFile.CompilationProblems?.Exists(cp => cp is OeCompilationError) ?? false) {
                    yield return new OeFile(previousFile);
                }
            }
        }
        
        /// <summary>
        /// List of the source file that are otherwise unchanged by need to be rebuild because they have new targets not present in the last build
        /// </summary>
        /// <param name="filesToBuildWithSetTargets"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFileToBuild> GetSourceFilesToRebuildBecauseTheyHaveNewTargets(PathList<IOeFileToBuild> filesToBuildWithSetTargets, PathList<IOeFileBuilt> previousFilesBuilt) {
            foreach (var newFile in filesToBuildWithSetTargets.Where(file => file.State == OeFileState.Unchanged)) {
                var previousFile = previousFilesBuilt[newFile.Path];
                var previouslyCreatedTargets = previousFile.Targets.ToNonNullEnumerable().Select(t => t.GetTargetPath()).ToList();
                foreach (var targetPath in newFile.TargetsToBuild.ToNonNullEnumerable().Select(t => t.GetTargetPath())) {
                    if (!previouslyCreatedTargets.Exists(prevTarget => prevTarget.PathEquals(targetPath))) {
                        yield return newFile;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Get a list of previously built files that are now deleted, their targets should be removed
        /// </summary>
        /// <param name="currentSourceListing"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFileBuilt> GetBuiltFilesDeletedSincePreviousBuild(PathList<IOeFile> currentSourceListing, PathList<IOeFileBuilt> previousFilesBuilt) {
            foreach (var previousFile in previousFilesBuilt.Where(f => f.State != OeFileState.Deleted)) {
                if (!currentSourceListing.Contains(previousFile.Path)) {
                    var previousFileCopy = new OeFileBuilt(previousFile) {
                        State = OeFileState.Deleted
                    };
                    yield return previousFileCopy;
                }
            }
        }
        
        /// <summary>
        /// Get a list of previously built files, still existing, but with targets that no longer exist and should be removed
        /// </summary>
        /// <param name="currentFilesBuilt"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        internal static IEnumerable<IOeFileBuilt> GetBuiltFilesWithOldTargetsToRemove(PathList<IOeFileToBuild> currentFilesBuilt, PathList<IOeFileBuilt> previousFilesBuilt) {
            var finalFileTargets = new List<AOeTarget>();
            foreach (var newFile in currentFilesBuilt.Where(file => file.State == OeFileState.Unchanged || file.State == OeFileState.Modified)) {
                var previousFile = previousFilesBuilt[newFile.Path];
                if (previousFile == null) {
                    throw new Exception($"Could not find the history of a now unchanged or modified file, something is wrong! File: {newFile}");
                }
                if (previousFile.State == OeFileState.Deleted) {
                    continue;
                }
                finalFileTargets.Clear();
                bool isFileWithTargetsToDelete = false;
                var newCreateTargets = newFile.TargetsToBuild.ToNonNullEnumerable().Select(t => t.GetTargetPath()).ToList();
                foreach (var previousTarget in previousFile.Targets.ToNonNullEnumerable()) {
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
                    previousFileCopy.State = newFile.State;
                    yield return previousFileCopy;
                }
            }
        }
    }
}