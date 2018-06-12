using System;
using System.IO;
using System.Text;
using Oetools.Packager.Core.Config;
using Oetools.Packager.Core.Exceptions;
using Oetools.Utilities.Lib;

namespace Oetools.Packager.Core.Execution {

    public class ProExecutionDeploymentHook : ProExecution {

        public override ExecutionType ExecutionType {
            get { return ExecutionType.DeploymentHook; }
        }

        public string DeploymentSourcePath { get; set; }

        public int DeploymentStep { get; set; }

        public string FileDeploymentHook { get; set; }

        public ProExecutionDeploymentHook(IEnvExecution env) : base(env) {}

        protected override void SetExecutionInfo() {
            base.SetExecutionInfo();

            if (string.IsNullOrEmpty(FileDeploymentHook) || !File.Exists(FileDeploymentHook)) {
                throw new ExecutionParametersException("Could not start deployment hook procedure, the program was not found : " + (FileDeploymentHook ?? "No file specified"));
            }

            var hookProc = new StringBuilder();
            hookProc.AppendLine("&SCOPED-DEFINE StepNumber " + DeploymentStep);
            hookProc.AppendLine("&SCOPED-DEFINE SourceDirectory " + DeploymentSourcePath.PreProcQuoter());
            var envCompil = Env as EnvExecutionCompilation;
            hookProc.AppendLine("&SCOPED-DEFINE DeploymentDirectory " + (envCompil != null ? envCompil.TargetDirectory : "").PreProcQuoter());
            
            var fileToExecute = "hook_" + DateTime.Now.ToString("yyMMdd_HHmmssfff") + ".p";
            var encoding = TextEncodingDetect.GetFileEncoding(FileDeploymentHook);
            File.WriteAllText(Path.Combine(_localTempDir, fileToExecute), Utils.ReadAllText(FileDeploymentHook, encoding).Replace(@"/*<inserted_3P_values>*/", hookProc.ToString()), encoding);

            SetPreprocessedVar("CurrentFilePath", fileToExecute.PreProcQuoter());
        }
    }
}