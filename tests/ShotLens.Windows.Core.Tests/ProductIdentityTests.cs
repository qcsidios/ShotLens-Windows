using ShotLens.Windows.Core;

namespace ShotLens.Windows.Core.Tests;

public sealed class ProductIdentityTests
{
    [Fact]
    public void Stable_identity_matches_existing_installation()
    {
        Assert.Equal("ShotLens", ProductIdentity.Name);
        Assert.Equal("ShotLens.Windows.App.exe", ProductIdentity.ExecutableName);
        Assert.Equal("ShotLens", ProductIdentity.StableSettingsDirectoryName);
    }
}
