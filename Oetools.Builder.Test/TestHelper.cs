#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TestHelper.cs) is part of Oetools.Builder.Test.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Oetools.Utilities.Openedge;
using Oetools.Utilities.Openedge.Exceptions;

namespace Oetools.Builder.Test {
    public static class TestHelper {
        
        private static readonly string TestFolder = Path.Combine(AppContext.BaseDirectory, "Tests");

        public static bool GetDlcPath(out string dlcPath) {
            try {
                dlcPath = UoeUtilities.GetDlcPathFromEnv();
            } catch (UoeDlcNotFoundException) {
                Console.WriteLine("Cancelling test, DLC environment variable not found!");
                dlcPath = null;
                return false;
            }
            if (string.IsNullOrEmpty(dlcPath)) {
                Console.WriteLine("Cancelling test, DLC environment variable not found!");
                return false;
            }
            if (!Directory.Exists(dlcPath)) {
                Console.WriteLine("Cancelling test, DLC environment variable not found!");
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
        
        
        public static IEnumerable<Type> GetTypesInNamespace(string assembly, string nameSpace) {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => assembly == null || a.FullName.StartsWith(assembly))
                .SelectMany(t => t.GetTypes())
                .Where(t => t.Namespace?.StartsWith(nameSpace, StringComparison.OrdinalIgnoreCase) ?? false);
        }
        
        public static bool CanBeNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}