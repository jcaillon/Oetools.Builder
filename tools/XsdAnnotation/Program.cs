#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (Program.cs) is part of ConsoleApplication1.
// 
// ConsoleApplication1 is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ConsoleApplication1 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ConsoleApplication1. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XsdAnnotator {
    public class Program {
        
        private static List<Assembly> _loadedAssemblies;

        public static int Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver;
            
            if (args == null || args.Length < 2) {
                Console.Error.WriteLine("Expected 2 arguments! [XsdPath] [AssemblyFolder]");
                return 1;
            }

            string xsdPath = args[0];
            string assemblyFolderPath = args[1];

            if (!Directory.Exists(assemblyFolderPath)) {
                Console.Error.WriteLine($"The directory does not exist : {assemblyFolderPath}");
                return 1;
            }
            Console.WriteLine($"The assemblies directory used is : {assemblyFolderPath}");

            if (!File.Exists(xsdPath)) {
                Console.Error.WriteLine($"The file does not exist : {xsdPath}");
                return 1;
            }
            Console.WriteLine($"The xsd to annotate is : {xsdPath}");

            var existingTypes = new List<Type>();

            _loadedAssemblies = new List<Assembly>();
            foreach (var dllPath in Directory.EnumerateFiles(assemblyFolderPath, "*.dll", SearchOption.TopDirectoryOnly)) {
                _loadedAssemblies.Add(Assembly.Load(File.ReadAllBytes(dllPath)));
                Console.WriteLine($"Loaded assembly into memory : {_loadedAssemblies.Last().GetName().FullName}");
            }

            foreach (var assembly in _loadedAssemblies) {
                existingTypes.AddRange(assembly.GetExportedTypes());
            }

            var annotator = new XsdAnnotate(existingTypes);
            try {
                annotator.Annotate(xsdPath, xsdPath);
            } catch (Exception e) {
                Console.Error.WriteLine(e);
                return 1;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Called when the resolution of an assembly fails, gives us the opportunity to feed the required asssembly
        /// to the program
        /// </summary>
        private static Assembly AssemblyResolver(object sender, ResolveEventArgs args) {
            // see code https://msdn.microsoft.com/en-us/library/d4tc2453(v=vs.110).aspx
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);

            if (assembly != null) {
                return assembly;
            }

            return  _loadedAssemblies.FirstOrDefault(a => a.FullName == args.Name);
        }
    }
}