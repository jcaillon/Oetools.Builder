using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Oetools.Packager.Core.Exceptions;

namespace Oetools.Packager.Core.Execution {

    internal class ProExecutionProlint : ProExecutionHandleCompilation {

        public override ExecutionType ExecutionType { get { return ExecutionType.Prolint; } }

        public string ProlintStartupProcedurePath { get; set; }

        private string _prolintOutputPath;

        protected override void CheckParameters() {
            base.CheckParameters();

            // Check if the startprolint procedure exists or create it from resources
            if (!File.Exists(ProlintStartupProcedurePath)) {
                try {
                    File.WriteAllBytes(ProlintStartupProcedurePath, Env.ProgramStartProlint);
                } catch (Exception e) {
                    throw new ExecutionParametersException("Could not write the prolint entry point procedure, check wirting rights for the file : " + ProlintStartupProcedurePath, e);
                }
            }
        }

        protected override void SetExecutionInfo() {
            base.SetExecutionInfo();

            // prolint, we need to copy the StartProlint program
            var fileToExecute = "prolint_" + DateTime.Now.ToString("yyMMdd_HHmmssfff") + ".p";
            _prolintOutputPath = Path.Combine(_localTempDir, "prolint.log");

            StringBuilder prolintProgram = new StringBuilder();
            prolintProgram.AppendLine("&SCOPED-DEFINE PathFileToProlint " + Files.First().CompiledSourcePath.PreProcQuoter());
            prolintProgram.AppendLine("&SCOPED-DEFINE PathProlintOutputFile " + _prolintOutputPath.PreProcQuoter());
            prolintProgram.AppendLine("&SCOPED-DEFINE PathToStartProlintProgram " + ProlintStartupProcedurePath.PreProcQuoter());
            prolintProgram.AppendLine("&SCOPED-DEFINE PathActualFilePath " + Files.First().SourcePath.PreProcQuoter());

            /*
            prolintProgram.AppendLine("&SCOPED-DEFINE UserName " + Config.Instance.UserName.PreProcQuoter());
            var filename = Npp.CurrentFileInfo.FileName;
            if (FileCustomInfo.Contains(filename)) {
                var fileInfo = FileCustomInfo.GetLastFileTag(filename);
                prolintProgram.AppendLine("&SCOPED-DEFINE FileApplicationName " + fileInfo.ApplicationName.PreProcQuoter());
                prolintProgram.AppendLine("&SCOPED-DEFINE FileApplicationVersion " + fileInfo.ApplicationVersion.PreProcQuoter());
                prolintProgram.AppendLine("&SCOPED-DEFINE FileWorkPackage " + fileInfo.WorkPackage.PreProcQuoter());
                prolintProgram.AppendLine("&SCOPED-DEFINE FileBugID " + fileInfo.BugId.PreProcQuoter());
                prolintProgram.AppendLine("&SCOPED-DEFINE FileCorrectionNumber " + fileInfo.CorrectionNumber.PreProcQuoter());
                prolintProgram.AppendLine("&SCOPED-DEFINE FileDate " + fileInfo.CorrectionDate.PreProcQuoter());

                prolintProgram.AppendLine("&SCOPED-DEFINE ModificationTagOpening " + ModificationTag.ReplaceTokens(fileInfo, ModificationTagTemplate.Instance.TagOpener).PreProcQuoter());
                prolintProgram.AppendLine("&SCOPED-DEFINE ModificationTagEnding " + ModificationTag.ReplaceTokens(fileInfo, ModificationTagTemplate.Instance.TagCloser).PreProcQuoter());
            }
            prolintProgram.AppendLine("&SCOPED-DEFINE PathDirectoryToProlint " + Updater<ProlintUpdaterWrapper>.Instance.ApplicationFolder.PreProcQuoter());
            prolintProgram.AppendLine("&SCOPED-DEFINE PathDirectoryToProparseAssemblies " + Updater<ProparseUpdaterWrapper>.Instance.ApplicationFolder.PreProcQuoter());
            */

            var encoding = TextEncodingDetect.GetFileEncoding(ProlintStartupProcedurePath);
            try {
                File.WriteAllText(Path.Combine(_localTempDir, fileToExecute), Utils.ReadAllText(ProlintStartupProcedurePath, encoding).Replace(@"/*<inserted_3P_values>*/", prolintProgram.ToString()), encoding);
            } catch (Exception e) {
                throw new ExecutionParametersException("Could not write the prolint entry point procedure, check wirting rights for the file : " + ProlintStartupProcedurePath, e);
            }
            
            SetPreprocessedVar("CurrentFilePath", fileToExecute.PreProcQuoter());
        }

        protected override Dictionary<string, List<FileError>> GetErrorsList(Dictionary<string, string> changePaths) {
            return ReadErrorsFromFile(_prolintOutputPath, true, changePaths);
        }
    }
}