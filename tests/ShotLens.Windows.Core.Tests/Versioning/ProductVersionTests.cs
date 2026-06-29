using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Tests.Versioning;

public sealed class ProductVersionTests
{
    [Fact]
    public void Reads_and_parses_version_file()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "v0.2.0-beta.1\n");

            var version = ProductVersion.Load(path);

            Assert.Equal(SemanticVersion.Parse("v0.2.0-beta.1"), version);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Missing_version_file_fails_instead_of_using_a_fallback()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-VERSION");

        Assert.Throws<FileNotFoundException>(() => ProductVersion.Load(path));
    }
}
