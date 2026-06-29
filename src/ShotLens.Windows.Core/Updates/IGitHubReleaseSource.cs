namespace ShotLens.Windows.Core.Updates;

public interface IGitHubReleaseSource
{
    Task<IReadOnlyList<GitHubRelease>> GetReleasesAsync(
        CancellationToken cancellationToken);
}
