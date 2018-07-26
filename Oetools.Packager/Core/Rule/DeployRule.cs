namespace Oetools.Packager.Core {
    public abstract class DeployRule : IDeployRule {
        
        /// <summary>
        /// Unique identifier for this rule
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        ///     Step to which the rule applies : 0 = compilation, 1 = deployment of all files, 2+ = extra
        /// </summary>
        public byte Step { get; set; }
        
        

        /// <summary>
        ///     The line from which we read this info, allows to sort by line
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        ///     the full file path in which this rule can be found
        /// </summary>
        public string Source { get; set; }
    }
}