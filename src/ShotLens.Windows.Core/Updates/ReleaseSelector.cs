using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Updates;

public static class ReleaseSelector
{
    public static AvailableUpdate? SelectLatest(
        SemanticVersion currentVersion,
        UpdateChannel channel,
        IEnumerable<GitHubRelease> releases) =>
        releases
            .Where(release => IsEligible(release, currentVersion, channel))
            .Select(ToAvailableUpdate)
            .Where(update => update is not null)
            .OrderByDescending(update => update!.Version)
            .FirstOrDefault();

    private static bool IsEligible(
        GitHubRelease release,
        SemanticVersion currentVersion,
        UpdateChannel channel)
    {
        if (release.Draft || release.Version.CompareTo(currentVersion) <= 0)
        {
            return false;
        }

        var versionIsPrerelease = release.Version.BetaNumber is not null;
        if (release.Prerelease != versionIsPrerelease)
        {
            return false;
        }

        return channel == UpdateChannel.Beta || !release.Prerelease;
    }

    private static AvailableUpdate? ToAvailableUpdate(GitHubRelease release)
    {
        var expectedName = release.Prerelease
            ? $"ShotLens-Beta-{release.Version}-Setup.exe"
            : $"ShotLens-Windows-{release.Version}-Setup.exe";
        var installer = release.Assets.SingleOrDefault(
            asset => string.Equals(asset.Name, expectedName, StringComparison.Ordinal));

        return installer is null
            ? null
            : new AvailableUpdate(release.Version, release.ReleasePage, installer);
    }
}
