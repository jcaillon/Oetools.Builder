namespace Oetools.Packager.Core {
    public interface IDeployFilterRule : IDeployRule {
        /// <summary>
        ///     true if the rule is about including a file (+) false if about excluding (-)
        /// </summary>
        bool Include { get; set; }

        /// <summary>
        ///     Pattern to match in the source (as a regular expression)
        /// </summary>
        string SourcePattern { get; set; }
    }
}