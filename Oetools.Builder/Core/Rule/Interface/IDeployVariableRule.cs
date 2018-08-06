namespace Oetools.Builder.Core {
    public interface IDeployVariableRule : IDeployRule {
        /// <summary>
        ///     the name of the variable, format &lt;XXX&gt;
        /// </summary>
        string VariableName { get; set; }

        /// <summary>
        ///     The path that should replace the variable &lt;XXX&gt;
        /// </summary>
        string Path { get; set; }
    }
}