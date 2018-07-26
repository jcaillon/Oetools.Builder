namespace Oetools.Packager.Core {
    public class DeployFilterRule : DeployRule, IDeployFilterRule {
        /// <summary>
        ///     true if the rule is about including a file (+) false if about excluding (-)
        /// </summary>
        public bool Include { get; set; }

        /// <summary>
        ///     Pattern to match in the source path
        /// </summary>
        public string SourcePattern { get; set; }

        /// <summary>
        ///     Pattern to match in the source (as a regular expression)
        /// </summary>
        public string RegexSourcePattern { get; set; }
    }
}