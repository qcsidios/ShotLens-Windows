namespace ShotLens.Windows.Core.Versioning;

public static class ProductVersion
{
    public static SemanticVersion Load(string path)
    {
        var value = File.ReadAllText(path).Trim();
        return SemanticVersion.Parse(value);
    }
}
