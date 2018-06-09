﻿#region header

// ========================================================================
// Copyright (c) 2017 - Julien Caillon (julien.caillon@gmail.com)
// This file (FileToCompile.cs) is part of csdeployer.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Oetools.Packager.Core {
    /// <summary>
    ///     This class represents a file that needs to be compiled
    /// </summary>
    public class FileToCompile {
        /// <summary>
        ///     The path to the source that needs to be compiled
        /// </summary>
        public string SourcePath { get; set; }

        // stores temporary path used during the compilation
        public string CompiledSourcePath { get; set; }

        public string CompilationOutputDir { get; set; }
        public string CompOutputR { get; set; }
        public string CompOutputXrf { get; set; }
        public string CompOutputLis { get; set; }
        public string CompOutputDbg { get; set; }

        /// <summary>
        ///     This temporary file is actually a log with only FileId activated just before the compilation
        ///     and deactivated just after; this allows us to know which file were used to compile the source
        /// </summary>
        public string CompOutputFileIdLog { get; set; }

        /// <summary>
        ///     Temporary file that list the "table\tCRC" for each referenced table in the output .r
        /// </summary>
        public string CompOutputRefTables { get; set; }

        /// <summary>
        ///     List of errors
        /// </summary>
        public List<FileError> Errors { get; set; }

        /// <summary>
        ///     represents the source file (i.e. includes) used to generate a given .r code file
        /// </summary>
        public List<string> RequiredFiles { get; private set; }

        /// <summary>
        ///     represent the tables that were referenced in a given .r code file
        /// </summary>
        public List<TableCrc> RequiredTables { get; private set; }

        /// <summary>
        ///     Returns the base file name (set in constructor)
        /// </summary>
        public string BaseFileName { get; private set; }

        /// <summary>
        ///     Size of the file to compile (set in constructor)
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        ///     Constructor
        /// </summary>
        public FileToCompile(string sourcePath) : this(sourcePath, -1) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        public FileToCompile(string sourcePath, long size) {
            SourcePath = sourcePath;
            BaseFileName = Path.GetFileNameWithoutExtension(sourcePath);
            if (size > -1)
                Size = size;
            else
                try {
                    Size = new FileInfo(sourcePath).Length;
                } catch (Exception) {
                    Size = 0;
                }
        }

        /// <summary>
        ///     File the references used to compile this file (include and tables)
        /// </summary>
        public void ReadAnalysisResults() {
            // read RCodeTableReferenced
            if (!string.IsNullOrEmpty(CompOutputRefTables)) {
                RequiredTables = new List<TableCrc>();
                Utils.ForEachLine(CompOutputRefTables, new byte[0], (i, line) => {
                    var split = line.Split('\t');
                    if (split.Length == 2) {
                        var crc = split[1].Trim();
                        var qualifiedName = split[0].Trim();
                        if (!crc.Equals("0"))
                            if (!RequiredTables.Exists(tableCrc => tableCrc.QualifiedTableName.EqualsCi(qualifiedName)))
                                RequiredTables.Add(new TableCrc {
                                    QualifiedTableName = qualifiedName,
                                    Crc = crc
                                });
                    }
                }, Encoding.Default);
                CompOutputRefTables = null;
            }

            // read RCodeSourceFileUsed
            if (!string.IsNullOrEmpty(CompOutputFileIdLog)) {
                var compiledSourcePathBaseFileName = Path.GetFileName(CompiledSourcePath);
                var references = new HashSet<string>();
                Utils.ForEachLine(CompOutputFileIdLog, new byte[0], (i, line) => {
                    try {
                        // we want to read this kind of line :
                        // [17/04/09@16:44:14.372+0200] P-009532 T-007832 2 4GL FILEID         Open E:\Common\CommonObj.cls ID=33
                        // skip until the 5th space
                        var idx = 0;
                        var nbFoundSpace = 0;
                        do {
                            if (line[idx] == ' ') {
                                nbFoundSpace++;
                                if (nbFoundSpace == 5)
                                    break;
                            }

                            idx++;
                        } while (idx < line.Length);

                        idx++;
                        // the next thing we read should be FILEID
                        if (!line.Substring(idx, 6).Equals("FILEID"))
                            return;
                        idx += 6;
                        // skip all whitespace
                        while (idx < line.Length) {
                            if (line[idx] != ' ')
                                break;
                            idx++;
                        }

                        // now we should read Open
                        if (idx > line.Length - 1 || !line.Substring(idx, 5).Equals("Open "))
                            return;
                        idx += 5;
                        // find the last index of a white space
                        var lastIdx = line.Length - 1;
                        do {
                            if (line[lastIdx] == ' ')
                                break;
                            lastIdx--;
                        } while (lastIdx >= 0);

                        var newFile = line.Substring(idx, lastIdx - idx);

                        if (!references.Contains(newFile) && !newFile.EndsWith(".r", StringComparison.CurrentCultureIgnoreCase) && !newFile.EndsWith(".pl", StringComparison.CurrentCultureIgnoreCase) && !newFile.StartsWith(CompilationOutputDir, StringComparison.CurrentCultureIgnoreCase) && !Path.GetFileName(newFile).Equals(compiledSourcePathBaseFileName))
                            references.Add(newFile);
                    } catch (Exception) {
                        // wrong line format
                    }
                }, Encoding.Default);
                RequiredFiles = references.ToList();
                CompOutputFileIdLog = null;
            }
        }
    }

    #region TableCrc

    /// <summary>
    ///     This class represent the tables that were referenced in a given .r code file
    /// </summary>
    [Serializable]
    public class TableCrc {
        public string QualifiedTableName { get; set; }
        public string Crc { get; set; }
    }

    #endregion
    
}