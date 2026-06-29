namespace ShotLens.Windows.Core.Tests.Packaging;

public sealed class InstallerIdentityTests
{
    [Fact]
    public void Inno_script_contains_separate_stable_and_beta_identities()
    {
        var script = File.ReadAllText(RepositoryFile("installer", "ShotLens.iss"));

        Assert.Contains("{5A7B81D7-4566-4AB5-8A62-2CB0C96F0619}", script);
        Assert.Contains("{F6F10CFD-3739-4319-A150-E59C25CA3325}", script);
        Assert.Contains(@"{autopf}\ShotLens", script);
        Assert.Contains(@"{autopf}\ShotLens Beta", script);
        Assert.Contains("ShotLens-Windows-", script);
        Assert.Contains("ShotLens-Beta-", script);
    }

    private static string RepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VERSION")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. parts]);
    }
}
