namespace Oetools.Builder.Core {
    /// <summary>
    ///     Types of deploy, used during rules sorting
    /// </summary>
    public enum DeployType : byte {
        None = 0,
        Delete = 1,
        DeleteFolder = 2,

        DeleteInProlib = 10,
        Prolib = 11,
        Zip = 12,
        Cab = 13,
        Ftp = 14,
        // every item above are treated in "packs"

        CopyFolder = 21,

        // Copy / move should always be last
        Copy = 30,
        Move = 31
    }
}