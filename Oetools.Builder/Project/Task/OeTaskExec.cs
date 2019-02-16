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

    /// <summary>
    /// This task starts a new external process.
    /// </summary>
    [Serializable]
    [XmlRoot("Exec")]
    public class OeTaskExec : AOeTask {

        /// <summary>
        /// The path to the executable file.
        /// </summary>
        [XmlElement("ExecutableFilePath")]
        public string ExecutableFilePath { get; set; }

        /// <summary>
        /// The command line parameters for the execution.
        /// </summary>
        /// <remarks>
        /// Write the arguments as you would write them in a command line interface.
        /// Double quotes can be escaped by doubling them.
        /// e.g.: -opt value1 -opt2 "my value2" -opt3 "my ""quoted"" value3"
        /// </remarks>
        [XmlElement("Parameters")]
        public string Parameters { get; set; }

        /// <summary>
        /// Hide the execution (do not show a window).
        /// </summary>
        [XmlElement(ElementName = "HiddenExecution")]
        public bool? HiddenExecution { get; set; }

        /// <summary>
        /// The maximum time in milliseconds before aborting the execution.
        /// </summary>
        /// <remarks>
        /// Defaults to 0 which does not limit the time.
        /// </remarks>
        [XmlElement(ElementName = "MaxTimeOut")]
        public int? MaxTimeOut { get; set; }

        /// <summary>
        /// Do not redirect the executable standard and error output to the log.
        /// </summary>
        [XmlElement(ElementName = "DoNotRedirectOutput")]
        public bool? DoNotRedirectOutput { get; set; }

        /// <summary>
        /// Do not consider exit code different than 0 as a failed execution.
        /// </summary>
        [XmlElement(ElementName = "IgnoreExitCode")]
        public bool? IgnoreExitCode { get; set; }

        /// <summary>
        /// Finish this task in error if the executable wrote in the error output stream.
        /// </summary>
        [XmlElement(ElementName = "FailOnErrorOutput")]
        public bool? FailOnErrorOutput { get; set; }

        /// <summary>
        /// The directory to use as the working directory for the execution.
        /// </summary>
        [XmlElement("WorkingDirectory")]
        public string WorkingDirectory { get; set; }

        public override void Validate() {
            if (string.IsNullOrEmpty(ExecutableFilePath)) {
                throw new TaskValidationException(this, $"This task needs the following property to be defined : {GetType().GetXmlName(nameof(ExecutableFilePath))}");
            }
        }

        protected override void ExecuteInternal() {
            var redirectOutput = !(DoNotRedirectOutput ?? false);

            var proc = new ProcessIo(ExecutableFilePath) {
                RedirectOutput = redirectOutput,
                Log = Log,
                CancelToken = CancelToken
            };

            Log?.Debug($"Executing program {ExecutableFilePath}");
            Log?.Debug($"With parameters {Parameters}");
            Log?.Debug($"{(HiddenExecution ?? false ? "Hide execution" : "Show execution")} and {(redirectOutput ? "redirect output to info log" : "don't redirect output")}");

            try {
                proc.Execute(new ProcessArgs().AppendFromQuotedArgs(Parameters), HiddenExecution ?? false, MaxTimeOut ?? 0);
            } catch (Exception e) {
                throw new TaskExecutionException(this, $"Failed to execute {ExecutableFilePath.PrettyQuote()} with parameters {Parameters.PrettyQuote()}", e);
            }

            if (redirectOutput) {
                Log?.Info(proc.StandardOutput.ToString());
            }

            if (proc.ErrorOutput.Length > 0) {
                if (FailOnErrorOutput ?? false) {
                    throw new TaskExecutionException(this, $"The execution of {ExecutableFilePath.PrettyQuote()} has content in the error stream :{Environment.NewLine}{proc.ErrorOutput}");
                }
                Log?.Error(proc.ErrorOutput.ToString());
            }

            if (!(IgnoreExitCode ?? false) && proc.ExitCode != 0) {
                throw new TaskExecutionException(this, $"The execution of {ExecutableFilePath.PrettyQuote()} with parameters {Parameters.PrettyQuote()} ended with the exit code {proc.ExitCode}");
            }
        }

        /// <inheritdoc cref="AOeTask.ExecuteTestModeInternal"/>
        protected override void ExecuteTestModeInternal() {
            // does nothing in test mode
        }
    }
}
