namespace Oetools.Packager.Core {
    public class DeployVariableRule : DeployRule, IDeployVariableRule {
        /// <summary>
        ///     the name of the variable, format &lt;XXX&gt;
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        ///     The path that should replace the variable &lt;XXX&gt;
        /// </summary>
        public string Path { get; set; }
    }
}