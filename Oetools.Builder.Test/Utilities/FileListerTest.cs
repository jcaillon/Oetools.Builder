#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (FileListerTest.cs) is part of Oetools.Builder.Test.
// 
// Oetools.Builder.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test.Utilities {
    
    [TestClass]
    public class FileListerTest {
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(Path.Combine(nameof(FileListerTest), Path.GetRandomFileName())));
                     
        [ClassInitialize]
        public static void Init(TestContext context) {
            Cleanup();
            Utils.CreateDirectoryIfNeeded(TestFolder);
        }


        [ClassCleanup]
        public static void Cleanup() {
            Utils.DeleteDirectoryIfExists(TestFolder, true);
        }

        private static GitManager _git = new GitManager();


        [TestMethod]
        public void ClassicListing_Test_File_State_other() {
            var repoDir = Path.Combine(TestFolder, "test_state");
            
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "sub"));
            File.WriteAllText(Path.Combine(repoDir, "file1"), "");
            File.WriteAllText(Path.Combine(repoDir, "sub", "file2"), "");
            File.WriteAllText(Path.Combine(repoDir, "sub", "file3"), "");
            File.WriteAllText(Path.Combine(repoDir, "file4"), "");

            var lister = new FileLister(repoDir) {
                UseLastWriteDateComparison = true,
                UseHashComparison = false,
                PreviousSourceFiles = null
            };
            
            var list = lister.GetFileList();
            Assert.AreEqual(4, list.Count, "count all");
            Assert.IsTrue(list.All(f => f.State == OeFileState.Added), "all added");

            lister.PreviousSourceFiles = list;
            
            File.Delete(Path.Combine(repoDir, "file4"));
            File.WriteAllText(Path.Combine(repoDir, "sub", "file2"), "content");
            
            list = lister.GetFileList();
            Assert.AreEqual(4, list.Count, "count all");
            Assert.AreEqual(1, list.Count(f => f.State == OeFileState.Modified), "1 modified");
            Assert.AreEqual(1, list.Count(f => f.State == OeFileState.Modified), "1 deleted");
            Assert.AreEqual(2, list.Count(f => f.State == OeFileState.Unchanged), "2 unchanged");

            lister.PreviousSourceFiles = list;
            
            File.WriteAllText(Path.Combine(repoDir, "file4"), "");
            
            list = lister.GetFileList();
            Assert.AreEqual(4, list.Count, "count all");
            Assert.AreEqual(1, list.Count(f => f.State == OeFileState.Added), "1 added");
            Assert.AreEqual(3, list.Count(f => f.State == OeFileState.Unchanged), "3 unchanged");

            lister.PreviousSourceFiles = list;
            
            File.Delete(Path.Combine(repoDir, "file4"));
            File.Delete(Path.Combine(repoDir, "sub", "file3"));
            
            list = lister.GetFileList();
            Assert.AreEqual(4, list.Count, "count all");
            Assert.AreEqual(2, list.Count(f => f.State == OeFileState.Deleted), "2 deleted");
            Assert.AreEqual(2, list.Count(f => f.State == OeFileState.Unchanged), "2 unchanged");
            
            lister.PreviousSourceFiles = list;
            
            File.WriteAllText(Path.Combine(repoDir, "file4"), "");
            
            list = lister.GetFileList();
            Assert.AreEqual(3, list.Count, "count all");
            Assert.AreEqual(1, list.Count(f => f.State == OeFileState.Added), "1 added");
            Assert.AreEqual(2, list.Count(f => f.State == OeFileState.Unchanged), "2 unchanged");
            Assert.IsTrue(!list.Exists(f => f.SourcePath.Contains("file3")));   
        }

        [TestMethod]
        public void ClassicListing_Test_File_State_modified() {
            var repoDir = Path.Combine(TestFolder, "test_state");
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "sub"));
            File.WriteAllText(Path.Combine(repoDir, "file1"), "");
            File.WriteAllText(Path.Combine(repoDir, "sub", "file2"), "");
            File.WriteAllText(Path.Combine(repoDir, "sub", "file3"), "");
            File.WriteAllText(Path.Combine(repoDir, "file4"), "");

            var lister = new FileLister(repoDir) {
                UseLastWriteDateComparison = false,
                UseHashComparison = false,
                PreviousSourceFiles = null
            };

            var list = lister.GetFileList();
            Assert.AreEqual(4, list.Count, "count all");
            Assert.IsTrue(list.All(f => f.State == OeFileState.Added), "all added");

            lister.PreviousSourceFiles = list;
            
            var newList = lister.GetFileList();
            Assert.IsTrue(list.All(f => f.State == OeFileState.Added), "make sure we didn't modify previous objects");
            Assert.AreEqual(4, newList.Count, "count all");
            Assert.IsTrue(newList.All(f => f.State == OeFileState.Unchanged), "all unchanged");
            
            // try modify
            File.WriteAllText(Path.Combine(repoDir, "file1"), "content");
            
            newList = lister.GetFileList();
            
            Assert.AreEqual(4, newList.Count, "count all");
            Assert.AreEqual(1, newList.Count(f => f.State == OeFileState.Modified), "1 modified");
            Assert.AreEqual(3, newList.Count(f => f.State == OeFileState.Unchanged), "3 unchanged");
            
            lister.PreviousSourceFiles = newList;
            
            // try modify same size
            File.WriteAllText(Path.Combine(repoDir, "file1"), "conten1");
            
            newList = lister.GetFileList();
            
            Assert.AreEqual(4, newList.Count, "count all");
            Assert.IsTrue(newList.All(f => f.State == OeFileState.Unchanged), "all unchanged because the size is the same and that's our only cirteria");
            
            // activate date comparison
            lister.UseLastWriteDateComparison = true;
            
            newList = lister.GetFileList();
            
            Assert.AreEqual(4, newList.Count, "count all");
            Assert.AreEqual(1, newList.Count(f => f.State == OeFileState.Modified), "1 modified");
            Assert.AreEqual(3, newList.Count(f => f.State == OeFileState.Unchanged), "3 unchanged");

            lister.PreviousSourceFiles = newList;
            
            newList = lister.GetFileList();
            
            Assert.AreEqual(4, newList.Count, "count all");
            Assert.IsTrue(newList.All(f => f.State == OeFileState.Unchanged), "all unchanged");
            
            // try hash
            lister.UseHashComparison = true;
            
            newList = lister.GetFileList();
            
            Assert.AreEqual(4, newList.Count, "count all");
            Assert.IsTrue(newList.All(f => f.State == OeFileState.Modified), "all modified because the old HASH was null");
            
            // set up the hash for all files
            newList.ForEach(f => FileLister.SetFileHash(f));
            lister.PreviousSourceFiles = newList;
                        
            newList = lister.GetFileList();
            
            Assert.AreEqual(4, newList.Count, "count all");
            Assert.IsTrue(newList.All(f => f.State == OeFileState.Unchanged), "all unchanged");
            
            // modify last HASH
            lister.PreviousSourceFiles[0].Hash = "fakehash";
            
            newList = lister.GetFileList();
            
            Assert.AreEqual(4, newList.Count, "count all");
            Assert.AreEqual(1, newList.Count(f => f.State == OeFileState.Modified), "1 modified");
            Assert.AreEqual(3, newList.Count(f => f.State == OeFileState.Unchanged), "3 unchanged");
            
            
        }
        
        [TestMethod]
        public void ClassicListing_Test_filter() {
            var repoDir = Path.Combine(TestFolder, "test_filter");
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "subfolder"));
            File.WriteAllText(Path.Combine(repoDir, "monfichier.txt"), "");
            File.WriteAllText(Path.Combine(repoDir, "subfolder", "monfichier.pls"), "");
            
            var lister = new FileLister(repoDir) {
                SourcePathFilters = new List<OeFilter> {
                    new OeFilter {
                        Exclude = "**((*)).pls"
                    }
                }
            };
            
            Assert.AreEqual(1, lister.GetFileList().Count, "no .pls file");
            Assert.AreEqual(Path.Combine(repoDir, "monfichier.txt"), lister.GetFileList()[0].SourcePath, "check file path");

            lister.SourcePathFilters = null;
            
            Assert.AreEqual(2, lister.GetFileList().Count, "all files now");

            lister.SourcePathFilters = new List<OeFilter> {
                new OeFilterRegex {
                    Exclude = "\\.[tT][xX][tT]"
                }
            };
            
            Assert.AreEqual(1, lister.GetFileList().Count, "no .txt file");
            Assert.AreEqual(Path.Combine(repoDir, "subfolder", "monfichier.pls"), lister.GetFileList()[0].SourcePath, "check my pls file");
        }
        
        
        [TestMethod]
        public void Listing_Test_Git_Filter() {
            var repoDir = Path.Combine(TestFolder, "local");
            var lister = new FileLister(repoDir) {
                UseLastWriteDateComparison = true,
                PreviousSourceFiles = null,
                SourcePathFilters = null,
                SourcePathGitFilter = null,
                UseHashComparison = true
            };

            // set up a local "remote" repo
            _git.SetCurrentDirectory(TestFolder);
            try {
                _git.ExecuteGitCommand($"init --bare remote.git");
            } catch (Exception) {
                Console.WriteLine("Cancelling test, can't find git!");
                return;
            }
           
            // clone empty remote
            try {
                _git.ExecuteGitCommand("clone remote.git local");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            _git.SetCurrentDirectory(repoDir);
            _git.ExecuteGitCommand(@"config user.email you@example.com");
            _git.ExecuteGitCommand(@"config user.name you");

            // new branch v1/dev
            try {
                _git.ExecuteGitCommand("checkout -b v1/dev");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }
            
            Assert.AreEqual(0, lister.GetFileList().Count, "It shouldn't list files in .git folder");
            
            // add some file
            File.WriteAllText(Path.Combine(repoDir, "init file"), "");
            
            Assert.AreEqual(1, lister.GetFileList().Count, "now we get one file");
            Assert.AreEqual(Path.Combine(repoDir, "init file"), lister.GetFileList()[0].SourcePath, "check that we get what we expect");
            
            lister.SourcePathGitFilter = new OeBuildConfiguration.OeGitFilter {
                IncludeOnlyFilesCommittedSinceLastMerge = true,
                IncludeOnlyModifiedFilesSinceLastCommit = true
            };
            
            Assert.AreEqual(1, lister.GetFileList().Count, "list one file with git commands instead");

            lister.SourcePathGitFilter.IncludeOnlyModifiedFilesSinceLastCommit = false;
            
            Assert.AreEqual(0, lister.GetFileList().Count, "we don't list files not committed");
            
            // add files to index
            _git.ExecuteGitCommand("add --all");
            _git.ExecuteGitCommand("commit -m \"v1/dev init\"");
           
            
            Assert.AreEqual(1, lister.GetFileList().Count, "now it's committed so we can list it again");
            Assert.AreEqual(Path.Combine(repoDir, "init file"), lister.GetFileList()[0].SourcePath, "check that we get full path");
            
            // new branch v1/dev/issue1
            try {
                _git.ExecuteGitCommand("checkout -b v1/ft/issue1");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }
            
            Assert.AreEqual(0, lister.GetFileList().Count, "we are on a new branch so we should not find files committed only into that branch!");
            
            // add some files
            File.WriteAllText(Path.Combine(repoDir, "new file1"), "");
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "folder", "subfolder"));
            File.WriteAllText(Path.Combine(repoDir, "folder", "subfolder", "newfile2"), "");
            
            Assert.AreEqual(0, lister.GetFileList().Count, "we still don't list files not committed");

            lister.SourcePathGitFilter.IncludeOnlyModifiedFilesSinceLastCommit = true;
            lister.SourcePathGitFilter.IncludeOnlyFilesCommittedSinceLastMerge = false;
            
            Assert.AreEqual(2, lister.GetFileList().Count, "but now we do");
            Assert.IsTrue(lister.GetFileList().Exists(f => f.SourcePath.Equals(Path.Combine(repoDir, "new file1"))), "check that we still get full path");
            
            // add files to index
            _git.ExecuteGitCommand("add --all");
            
            Assert.AreEqual(2, lister.GetFileList().Count, "we should also get those 2 files that are on the index but not committed");
            
            // commit
            _git.ExecuteGitCommand("commit -m \"v1/dev/issue fixing\"");
            
            
            Assert.AreEqual(0, lister.GetFileList().Count, "now we committed so we don't see them anymore...");
            
            lister.SourcePathGitFilter.IncludeOnlyFilesCommittedSinceLastMerge = true;

            Assert.AreEqual(2, lister.GetFileList().Count, "but now they are commmitted so we should see them with the other filter");
            
            // add some files
            File.WriteAllText(Path.Combine(repoDir, "newfile3"), "");
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "cool"));
            File.WriteAllText(Path.Combine(repoDir, "cool", "newfile4"), "");
            
            // add files to index
            _git.ExecuteGitCommand("add --all");
            _git.ExecuteGitCommand("commit -m \"v1/dev/issue still fixing\"");
            
            // add some file
            File.WriteAllText(Path.Combine(repoDir, "newfile5"), "");
            
            Assert.AreEqual(5, lister.GetFileList().Count, "with both filter, we should see 5 files");
            
            lister.SourcePathFilters = new List<OeFilter> {
                new OeFilter {
                    Exclude = "**file5"
                }
            };
            
            Assert.AreEqual(4, lister.GetFileList().Count, "we applied a source path filter");

            lister.SourcePathFilters = null;
            
            // push branch
            try {
                _git.ExecuteGitCommand("push -u origin v1/ft/issue1");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }
            
            Assert.AreEqual(5, lister.GetFileList().Count, "we should still see 5 files because the remote branch of the current branch doesn't count as merge");
            
            // commit
            _git.ExecuteGitCommand("add --all");
            _git.ExecuteGitCommand("commit -m \"v1/dev/issue fixing again\"");
            
            // merge feat onto dev
            try {
                _git.ExecuteGitCommand("checkout v1/dev");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }
            _git.ExecuteGitCommand("merge v1/ft/issue1");
            try {
                _git.ExecuteGitCommand("checkout v1/ft/issue1");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            Assert.AreEqual(0, lister.GetFileList().Count, "now we should have 0 files because the last commit is also the last merge commit and we have nothing in our working copy");
            
            // add some file
            File.WriteAllText(Path.Combine(repoDir, "newfile6"), "");
            // add files to index
            _git.ExecuteGitCommand("add --all");
            _git.ExecuteGitCommand("commit -m \"v1/dev/issue still fixing\"");
            
            // push branch
            try {
                _git.ExecuteGitCommand("push -u origin v1/ft/issue1");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }
            
            // checkout tag
            try {
                _git.ExecuteGitCommand("checkout origin/v1/ft/issue1");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }
            
            // delete branch v1/ft/issue1
            try {
                _git.ExecuteGitCommand("branch -D v1/ft/issue1");
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            lister.SourcePathGitFilter.IncludeOnlyModifiedFilesSinceLastCommit = false;
            
            Assert.AreEqual(1, lister.GetFileList().Count, "we are in detached mode, but on a commit that reference origin/v1/ft/issue1, so we presume we are on this branch and return the list of files modified from this branch to the last merge (which is 1 commit ago on v1/dev)");

            lister.SourcePathGitFilter.GitCurrentBranchName = "v1/ft/issue1";
            
            Assert.AreEqual(1, lister.GetFileList().Count, "Same thing, but this time we pointed the right branch");

        }
    }
}