namespace Oetools.Packager.Core {
    public enum DeploymentStep {
        CopyingReference,
        Listing,
        Compilation,
        DeployRCode,
        DeployFile,
        CopyingFinalPackageToDistant,
        BuildingWebclientDiffs,
        BuildingWebclientCompleteCab
    }
}