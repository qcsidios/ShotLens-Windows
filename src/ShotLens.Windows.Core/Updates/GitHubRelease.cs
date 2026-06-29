using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Updates;

public sealed record GitHubAsset(string Name, Uri DownloadUri);

public sealed record GitHubRelease(
    SemanticVersion Version,
    bool Draft,
    bool Prerelease,
    Uri ReleasePage,
    IReadOnlyList<GitHubAsset> Assets);

public sealed record AvailableUpdate(
    SemanticVersion Version,
    Uri ReleasePage,
    GitHubAsset Installer);
