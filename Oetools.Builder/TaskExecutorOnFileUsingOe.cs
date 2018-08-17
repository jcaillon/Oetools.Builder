// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutor.cs) is part of Oetools.Builder.
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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder {
       
    public class TaskExecutorOnFileBuildingSource : TaskExecutorOnFile, IDisposable {
        

        public List<OeFileBuilt> PreviouslyBuiltFiles { get; set; }
        
        public bool ForceFullRebuild { get; set; }
        
        public SourceFilesLister SourceLister { get; }

        public string SourceDirectory => SourceLister?.SourceDirectory;
        
        public TaskExecutorOnFileBuildingSource(List<OeTask> tasks) : base(tasks) { }

        /// <summary>
        /// Sets <see cref="TaskExecutorOnFile.TaskFiles"/> to the list of source files that need to be rebuilt
        /// </summary>
        public void SetFilesToRebuild() {
            TaskFiles = SourceLister.GetFileList();
            var extraFilesToRebuild = GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(Env, TaskFiles, PreviouslyBuiltFiles.Where(f => f is OeFileBuiltCompiled).Cast<OeFileBuiltCompiled>().ToList());
            foreach (var oeFile in SourceLister.FilterSourceFiles(extraFilesToRebuild)) {
                if (!TaskFiles.Exists(f => f.SourcePath.Equals(oeFile.SourcePath, StringComparison.CurrentCultureIgnoreCase)) && 
                    File.Exists(oeFile.SourcePath)) {
                    TaskFiles.Add(oeFile);
                }
            }
        }
        
        /// <summary>
        /// Returns a raw list of files that need to be rebuilt because :
        /// - one of their dependencies (source file, include) has been modified (modified/deleted)
        /// - one of their database references has been modified (modified/deleted)
        /// This list must then be filtered considering files that do not exist anymore or files that were already added to the rebuild list
        /// </summary>
        /// <param name="env"></param>
        /// <param name="filesModified"></param>
        /// <param name="previousFilesBuilt"></param>
        /// <returns></returns>
        public static IEnumerable<OeFile> GetListOfFileToCompileBecauseOfTableCrcChangesOrDependencesModification(EnvExecution env, IEnumerable<OeFile> filesModified, List<OeFileBuiltCompiled> previousFilesBuilt) {

            // add all previous source files that required now modified files
            foreach (var oeFile in filesModified) {
                foreach (var result in previousFilesBuilt.Where(prevf => prevf.RequiredFiles != null && prevf.RequiredFiles.Contains(oeFile.SourcePath, StringComparer.CurrentCultureIgnoreCase))) {
                    yield return result.GetDeepCopy();
                }
            }

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
        
        public void Dispose() {
              
        }
    }
}