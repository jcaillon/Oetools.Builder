#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskExec.cs) is part of Oetools.Builder.
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
using System.Xml.Serialization;
using Oetools.Builder.Exceptions;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Project.Task {
    
    [Serializable]
    [XmlRoot("Exec")]
    public class OeTaskExec : OeTask {
        
        [XmlElement("ExecutablePath")]
        public string ExecutablePath { get; set; }
            
        /// <summary>
        /// (you can use task variables in this string)
        /// </summary>
        [XmlElement("Parameters")]
        public string Parameters { get; set; }
            
        [XmlElement(ElementName = "HiddenExecution")]
        public bool? HiddenExecution { get; set; }
        
        [XmlElement(ElementName = "MaxTimeOut")]
        public int? MaxTimeOut { get; set; }
        
        [XmlElement(ElementName = "DoNotRedirectOutput")]
        public bool? DoNotRedirectOutput { get; set; }
            
        /// <summary>
        /// With this option, the task will not fail if the exit code is different of 0
        /// </summary>
        [XmlElement(ElementName = "IgnoreExitCode")]
        public bool? IgnoreExitCode { get; set; }
        
        [XmlElement(ElementName = "FailOnErrorOutput")]
        public bool? FailOnErrorOutput { get; set; }
            
        /// <summary>
        /// (default to output directory)
        /// </summary>
        [XmlElement("WorkingDirectory")]
        public string WorkingDirectory { get; set; }

        public override void Validate() {
            if (string.IsNullOrEmpty(ExecutablePath)) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined : {GetType().GetXmlName(nameof(ExecutablePath))}");
            }
        }

        protected override void ExecuteInternal() {
            var redirectOutput = !(DoNotRedirectOutput ?? false);
            
            var proc = new ProcessIo(ExecutablePath) {
                RedirectOutput = redirectOutput
            };
            
            Log?.Debug($"Executing program {ExecutablePath}");
            Log?.Debug($"With parameters {Parameters}");
            Log?.Debug($"{(HiddenExecution ?? false ? "Hide execution" : "Show execution")} and {(redirectOutput ? "redirect output to info log" : "don't redirect output")}");
            
            try {
                proc.Execute(Parameters, HiddenExecution ?? false, MaxTimeOut ?? 0);
            } catch (Exception e) {
                throw new TaskExecutionException(this, $"Failed to execute {ExecutablePath.PrettyQuote()} with parameters {Parameters.PrettyQuote()}", e);
            }

            if (redirectOutput) {
                Log?.Info(proc.StandardOutput.ToString());
            }

            if (proc.ErrorOutput.Length > 0) {
                if (FailOnErrorOutput ?? false) {
                    throw new TaskExecutionException(this, $"The execution of {ExecutablePath.PrettyQuote()} has content in the error stream :{Environment.NewLine}{proc.ErrorOutput}");
                }
                Log?.Error(proc.ErrorOutput.ToString());
            }

            if (!(IgnoreExitCode ?? false) && proc.ExitCode != 0) {
                throw new TaskExecutionException(this, $"The execution of {ExecutablePath.PrettyQuote()} with parameters {Parameters.PrettyQuote()} ended with the exit code {proc.ExitCode}");
            }
        }

        /// <inheritdoc cref="OeTask.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // does nothing in test mode
        }
    }
}