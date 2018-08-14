﻿#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TestHelper.cs) is part of Oetools.Utilities.Test.
// 
// Oetools.Utilities.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Utilities.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Utilities.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Oetools.Utilities.Openedge;

namespace Oetools.Builder.Test {
    public static class TestHelper {
        
        private static readonly string TestFolder = Path.Combine(AppContext.BaseDirectory, "Tests");

        public static bool GetDlcPath(out string dlcPath) {
            dlcPath = ProUtilities.GetDlcPathFromEnv();
            if (string.IsNullOrEmpty(dlcPath)) {
                return false;
            }
            if (!Directory.Exists(dlcPath)) {
                return false;
            }
            return true;
        }
        
        public static string GetTestFolder(string testName) {
            var path = Path.Combine(TestFolder, testName);
            Directory.CreateDirectory(path);
            return path;
        }
        
        public static TimeSpan Time(Action action) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
    }
}