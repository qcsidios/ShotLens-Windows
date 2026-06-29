using ShotLens.Windows.Core.Updates;
using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Tests.Updates;

public sealed class ReleaseSelectorTests
{
    [Fact]
    public void Stable_channel_ignores_drafts_prereleases_and_wrong_assets()
    {
        var releases = new[]
        {
            Release("v0.10.0", draft: true, prerelease: false, StableAsset("v0.10.0")),
            Release("v0.9.0-beta.2", draft: false, prerelease: true, BetaAsset("v0.9.0-beta.2")),
            Release("v0.8.0", draft: false, prerelease: false, "ShotLens-macOS-v0.8.0.dmg"),
            Release("v0.7.0", draft: false, prerelease: false, StableAsset("v0.7.0"))
        };

        var result = ReleaseSelector.SelectLatest(
            SemanticVersion.Parse("v0.2.0"),
            UpdateChannel.Stable,
            releases);

        Assert.Equal(SemanticVersion.Parse("v0.7.0"), result?.Version);
        Assert.Equal(StableAsset("v0.7.0"), result?.Installer.Name);
    }

    [Fact]
    public void Beta_channel_selects_a_later_beta()
    {
        var releases = new[]
        {
            Release("v0.2.0-beta.3", draft: false, prerelease: true, BetaAsset("v0.2.0-beta.3")),
            Release("v0.2.0-beta.2", draft: false, prerelease: true, BetaAsset("v0.2.0-beta.2"))
        };

        var result = ReleaseSelector.SelectLatest(
            SemanticVersion.Parse("v0.2.0-beta.1"),
            UpdateChannel.Beta,
            releases);

        Assert.Equal(SemanticVersion.Parse("v0.2.0-beta.3"), result?.Version);
        Assert.Equal(BetaAsset("v0.2.0-beta.3"), result?.Installer.Name);
    }

    [Fact]
    public void Beta_channel_can_offer_a_later_stable_release()
    {
        var releases = new[]
        {
            Release("v0.2.0", draft: false, prerelease: false, StableAsset("v0.2.0")),
            Release("v0.2.0-beta.9", draft: false, prerelease: true, BetaAsset("v0.2.0-beta.9"))
        };

        var result = ReleaseSelector.SelectLatest(
            SemanticVersion.Parse("v0.2.0-beta.8"),
            UpdateChannel.Beta,
            releases);

        Assert.Equal(SemanticVersion.Parse("v0.2.0"), result?.Version);
        Assert.Equal(StableAsset("v0.2.0"), result?.Installer.Name);
    }

    [Fact]
    public void Current_or_older_release_is_not_an_update()
    {
        var releases = new[]
        {
            Release("v0.2.0-beta.2", draft: false, prerelease: true, BetaAsset("v0.2.0-beta.2")),
            Release("v0.2.0-beta.1", draft: false, prerelease: true, BetaAsset("v0.2.0-beta.1"))
        };

        var result = ReleaseSelector.SelectLatest(
            SemanticVersion.Parse("v0.2.0-beta.2"),
            UpdateChannel.Beta,
            releases);

        Assert.Null(result);
    }

    [Fact]
    public void Asset_name_must_exactly_match_its_release_channel_and_version()
    {
        var releases = new[]
        {
            Release(
                "v0.2.0-beta.2",
                draft: false,
                prerelease: true,
                StableAsset("v0.2.0-beta.2")),
            Release(
                "v0.2.0-beta.3",
                draft: false,
                prerelease: true,
                "ShotLens-Beta-v0.2.0-beta.2-Setup.exe")
        };

        var result = ReleaseSelector.SelectLatest(
            SemanticVersion.Parse("v0.2.0-beta.1"),
            UpdateChannel.Beta,
            releases);

        Assert.Null(result);
    }

    private static GitHubRelease Release(
        string version,
        bool draft,
        bool prerelease,
        string assetName) =>
        new(
            SemanticVersion.Parse(version),
            draft,
            prerelease,
            new Uri($"https://github.test/releases/{version}"),
            [new GitHubAsset(assetName, new Uri($"https://github.test/assets/{assetName}"))]);

    private static string StableAsset(string version) =>
        $"ShotLens-Windows-{version}-Setup.exe";

    private static string BetaAsset(string version) =>
        $"ShotLens-Beta-{version}-Setup.exe";
}
