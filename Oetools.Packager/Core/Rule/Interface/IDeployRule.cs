namespace Oetools.Packager.Core {
    public interface IDeployRule {
        /// <summary>
        ///     Unique identifier for this rule
        /// </summary>
        ushort Id { get; set; }
        
        /// <summary>
        ///     Step to which the rule applies : 0 = compilation, 1 = deployment of all files, 2+ = extra
        /// </summary>
        byte Step { get; set; }
    }
}