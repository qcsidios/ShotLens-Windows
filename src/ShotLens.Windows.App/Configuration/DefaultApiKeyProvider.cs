using System.IO;

namespace ShotLens.Windows.App.Configuration;

public static class DefaultApiKeyProvider
{
    public static string? Load()
    {
        var environmentValue = Environment.GetEnvironmentVariable(
            "SHOTLENS_DEFAULT_API_KEY");
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        var path = Path.Combine(
            AppContext.BaseDirectory,
            "default-api-key.txt");
        return File.Exists(path)
            ? File.ReadAllText(path).Trim()
            : null;
    }
}
