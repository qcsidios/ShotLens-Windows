using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Tests.Versioning;

public sealed class SemanticVersionTests
{
    [Theory]
    [InlineData("v0.1.5", 0, 1, 5, null)]
    [InlineData("v0.2.0-beta.1", 0, 2, 0, 1)]
    [InlineData("v10.20.30-beta.42", 10, 20, 30, 42)]
    public void Parses_supported_versions(
        string value,
        int major,
        int minor,
        int patch,
        int? betaNumber)
    {
        var version = SemanticVersion.Parse(value);

        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(betaNumber, version.BetaNumber);
        Assert.Equal(value, version.ToString());
    }

    [Theory]
    [InlineData("0.2.0")]
    [InlineData("v0.2")]
    [InlineData("v0.2.0-beta")]
    [InlineData("v0.2.0-beta.0")]
    [InlineData("v0.2.0.1")]
    [InlineData("latest")]
    public void Rejects_unsupported_versions(string value)
    {
        Assert.Throws<FormatException>(() => SemanticVersion.Parse(value));
    }

    [Fact]
    public void Orders_beta_before_stable_and_compares_numbers_numerically()
    {
        var values = new[]
        {
            "v0.10.0",
            "v0.2.0",
            "v0.2.0-beta.2",
            "v0.1.5",
            "v0.2.0-beta.1"
        };

        var ordered = values
            .Select(SemanticVersion.Parse)
            .Order()
            .Select(version => version.ToString());

        Assert.Equal(
            ["v0.1.5", "v0.2.0-beta.1", "v0.2.0-beta.2", "v0.2.0", "v0.10.0"],
            ordered);
    }
}
