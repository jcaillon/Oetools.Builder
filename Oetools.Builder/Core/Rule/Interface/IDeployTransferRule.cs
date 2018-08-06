namespace Oetools.Builder.Core {
    public interface IDeployTransferRule : IDeployRule {
        /// <summary>
        ///     The type of transfer that should occur for this compilation path
        /// </summary>
        DeployType Type { get; }

        /// <summary>
        ///     if false, this should be the last rule applied to this file
        /// </summary>
        bool ContinueAfterThisRule { get; set; }

        /// <summary>
        ///     Pattern to match in the source path
        /// </summary>
        string SourcePattern { get; set; }

        /// <summary>
        ///     deploy target depending on the deploy type of this rule
        /// </summary>
        string DeployTarget { get; set; }
    }
}