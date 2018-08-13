// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (Builder.cs) is part of Oetools.Builder.
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

namespace Oetools.Builder {
    public class Builder {

        /*
        protected string GetCompilationTargetFolder() {
            // for *.cls files, as many *.r files are generated, we need to compile in a temp directory
            // we need to know which *.r files were generated for each input file
            // so each file gets his own sub tempDir
            var lastDeployment = Deployer.GetTransfersNeededForFile(fileToCompile.SourcePath, 0).Last();
            if (lastDeployment.DeployType != DeployType.Move ||
                Env.CompileForceUseOfTemp ||
                Path.GetExtension(fileToCompile.SourcePath ?? "").Equals(ExtCls))
                if (lastDeployment.DeployType != DeployType.Ftp &&
                    !string.IsNullOrEmpty(Env.TargetDirectory) && Env.TargetDirectory.Length > 2 && !Env.TargetDirectory.Substring(0, 2).EqualsCi(_localTempDir.Substring(0, 2))) {
                    if (!Directory.Exists(DistantTempDir)) {
                        var dirInfo = Directory.CreateDirectory(DistantTempDir);
                        dirInfo.Attributes |= FileAttributes.Hidden;
                    }
                    fileToCompile.CompilationOutputDir = Path.Combine(DistantTempDir, count.ToString());
                } else {
                    fileToCompile.CompilationOutputDir = localSubTempDir;
                }
            else fileToCompile.CompilationOutputDir = lastDeployment.TargetBasePath;
        }
        */

        public void Build() {
            
        }
    }
}