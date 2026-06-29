namespace ShotLens.Windows.Core.Tests.Packaging;

public sealed class WorkflowContractTests
{
    [Fact]
    public void Ci_workflow_never_creates_a_release()
    {
        var workflow = File.ReadAllText(
            RepositoryFile(".github", "workflows", "windows-ci.yml"));

        Assert.Contains("pull_request:", workflow);
        Assert.Contains("upload-artifact", workflow);
        Assert.DoesNotContain("gh release create", workflow);
    }

    [Fact]
    public void Release_workflow_supports_beta_and_stable_channels()
    {
        var workflow = File.ReadAllText(
            RepositoryFile(".github", "workflows", "windows-release.yml"));

        Assert.Contains("environment: release", workflow);
        Assert.Contains("SHOTLENS_DEFAULT_API_KEY", workflow);
        Assert.Contains("ShotLens-Beta-", workflow);
        Assert.Contains("ShotLens-Windows-", workflow);
        Assert.Contains("--prerelease", workflow);
        Assert.Contains("VERSION", workflow);
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
