using System;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Core.Rule {
    
    /// <summary>
    ///     Base class for transfer rules
    /// </summary>
    public abstract class DeployTransferRule : DeployRule, IDeployTransferRule {
        public static DeployTransferRule New(DeployType type) {
            switch (type) {
                case DeployType.Prolib:
                    return new DeployTransferRuleProlib();
                case DeployType.Cab:
                    return new DeployTransferRuleCab();
                case DeployType.Zip:
                    return new DeployTransferRuleZip();
                case DeployType.DeleteInProlib:
                    return new DeployTransferRuleDeleteInProlib();
                case DeployType.Ftp:
                    return new DeployTransferRuleFtp();
                case DeployType.Delete:
                    return new DeployTransferRuleDelete();
                case DeployType.Copy:
                    return new DeployTransferRuleCopy();
                case DeployType.Move:
                    return new DeployTransferRuleMove();
                case DeployType.CopyFolder:
                    return new DeployTransferRuleCopyFolder();
                case DeployType.DeleteFolder:
                    return new DeployTransferRuleDeleteFolder();
                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        /// <summary>
        ///     Returns the type of deployment needed to delete a file deployed with the given type
        /// </summary>
        public static DeployType GetDeletetionType(DeployType type) {
            switch (type) {
                case DeployType.Prolib:
                    return DeployType.DeleteInProlib;
                case DeployType.CopyFolder:
                    return DeployType.DeleteFolder;
                case DeployType.Copy:
                case DeployType.Move:
                    return DeployType.Delete;
                default:
                    return DeployType.None;
            }
        }

        /// <summary>
        ///     The type of transfer that should occur for this compilation path
        /// </summary>
        public virtual DeployType Type {
            get { return DeployType.Copy; }
        }

        /// <summary>
        ///     A transfer can either apply to a file or to a folder
        /// </summary>
        public virtual DeployTransferRuleTarget TargetType {
            get { return DeployTransferRuleTarget.File; }
        }

        /// <summary>
        ///     if false, this should be the last rule applied to this file
        /// </summary>
        public bool ContinueAfterThisRule { get; set; }

        /// <summary>
        ///     Pattern to match in the source path
        /// </summary>
        public string SourcePattern { get; set; }

        /// <summary>
        ///     deploy target depending on the deploy type of this rule
        /// </summary>
        public string DeployTarget { get; set; }

        /// <summary>
        ///     True if the rule is directly written as a regex and we want to replace matches in the source directory in the
        ///     deploy target (in that case it must start with ":")
        /// </summary>
        public bool ShouldDeployTargetReplaceDollar { get; set; }

        /// <summary>
        ///     Should return true if the rule is valid
        /// </summary>
        /// <param name="error"></param>
        public virtual bool IsValid(out string error) {
            error = null;
            if (!string.IsNullOrEmpty(SourcePattern) && !string.IsNullOrEmpty(DeployTarget)) return true;
            error = "The source or target path is empty";
            return false;
        }

        /// <summary>
        ///     Get a copy of this object
        /// </summary>
        /// <returns></returns>
        public virtual DeployTransferRule GetCopy() {
            var theCopy = New(Type);
            theCopy.Source = Source;
            theCopy.Line = Line;
            theCopy.Step = Step;
            theCopy.ContinueAfterThisRule = ContinueAfterThisRule;
            theCopy.SourcePattern = SourcePattern;
            return theCopy;
        }
    }

    /// <summary>
    ///     Delete file(s)
    /// </summary>
    public class DeployTransferRuleDelete : DeployTransferRule {
        public override DeployType Type {
            get { return DeployType.Delete; }
        }

        public override bool IsValid(out string error) {
            error = null;
            if (string.IsNullOrEmpty(SourcePattern)) {
                error = "The source path is empty";
                return false;
            }
            if (Step < 2) {
                error = "This deletion rule can only apply to steps >= 1";
                return false;
            }
            return true;
        }
    }

    /// <summary>
    ///     Delete folder(s) recursively
    /// </summary>
    public class DeployTransferRuleDeleteFolder : DeployTransferRule {
        public override DeployType Type {
            get { return DeployType.DeleteFolder; }
        }

        public override DeployTransferRuleTarget TargetType {
            get { return DeployTransferRuleTarget.Folder; }
        }

        public override bool IsValid(out string error) {
            error = null;
            if (string.IsNullOrEmpty(SourcePattern)) {
                error = "The source path is empty";
                return false;
            }
            if (Step < 2) {
                error = "This deletion rule can only apply to steps >= 1";
                return false;
            }
            return true;
        }
    }

    /// <summary>
    ///     Abstract class for PACK rules
    /// </summary>
    public abstract class DeployTransferRulePack : DeployTransferRule {
        public virtual string ArchiveExt {
            get { return ".arc"; }
        }

        public override bool IsValid(out string error) {
            if (!string.IsNullOrEmpty(DeployTarget) && !DeployTarget.ContainsFast(ArchiveExt)) {
                error = "The target path should be a file with the following extension " + ArchiveExt;
                return false;
            }
            return base.IsValid(out error);
        }
    }

    /// <summary>
    ///     Transfer file(s) to a .pl file
    /// </summary>
    public class DeployTransferRuleProlib : DeployTransferRulePack {
        public override DeployType Type {
            get { return DeployType.Prolib; }
        }

        public override string ArchiveExt {
            get { return ".pl"; }
        }
    }

    /// <summary>
    ///     Transfer file(s) to a .zip file
    /// </summary>
    public class DeployTransferRuleZip : DeployTransferRulePack {
        public override DeployType Type {
            get { return DeployType.Zip; }
        }

        public override string ArchiveExt {
            get { return ".zip"; }
        }
    }

    /// <summary>
    ///     Transfer file(s) to a .cab file
    /// </summary>
    public class DeployTransferRuleCab : DeployTransferRulePack {
        public override DeployType Type {
            get { return DeployType.Cab; }
        }

        public override string ArchiveExt {
            get { return ".cab"; }
        }
    }

    /// <summary>
    ///     Delete file(s) in a prolib file
    /// </summary>
    public class DeployTransferRuleDeleteInProlib : DeployTransferRulePack {
        public override DeployType Type {
            get { return DeployType.DeleteInProlib; }
        }

        public override string ArchiveExt {
            get { return ".pl"; }
        }

        public override bool IsValid(out string error) {
            error = null;
            if (string.IsNullOrEmpty(SourcePattern) || string.IsNullOrEmpty(DeployTarget)) {
                error = "The path to the .pl or the relative path within the .pl is empty";
                return false;
            }
            if (Step < 2) {
                error = "This deletion rule can only apply to step >= 1";
                return false;
            }
            if (!SourcePattern.EndsWith(ArchiveExt)) {
                error = "The source path should be a file with the following extension  " + ArchiveExt;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    ///     Send file(s) over FTP
    /// </summary>
    public class DeployTransferRuleFtp : DeployTransferRulePack {
        public override DeployType Type {
            get { return DeployType.Ftp; }
        }

        public override bool IsValid(out string error) {
            if (!string.IsNullOrEmpty(DeployTarget) && !DeployTarget.IsValidFtpAddress()) {
                error = "The target should have the following format ftp://user:pass@server:port/distantpath/ (with user/pass/port in option)";
                return false;
            }
            return base.IsValid(out error);
        }
    }

    /// <summary>
    ///     Copy folder(s) recursively
    /// </summary>
    public class DeployTransferRuleCopyFolder : DeployTransferRule {
        public override DeployType Type {
            get { return DeployType.CopyFolder; }
        }

        public override DeployTransferRuleTarget TargetType {
            get { return DeployTransferRuleTarget.Folder; }
        }

        public override bool IsValid(out string error) {
            if (Step < 2) {
                error = "The copy of folders can only apply to steps >= 1";
                return false;
            }
            return base.IsValid(out error);
        }
    }

    /// <summary>
    ///     Copy file(s)
    /// </summary>
    public class DeployTransferRuleCopy : DeployTransferRule {
        public override DeployType Type {
            get { return DeployType.Copy; }
        }
    }

    /// <summary>
    ///     Move file(s)
    /// </summary>
    public class DeployTransferRuleMove : DeployTransferRule {
        public override DeployType Type {
            get { return DeployType.Move; }
        }
    }
}