namespace ShotLens.Windows.Core;

public static class VersionInfo
{
    public static string Current { get; } = ReadVersion();

    public static string SemVer => Current.Trim().TrimStart('v', 'V');

    private static string ReadVersion()
    {
        foreach (var directory in CandidateDirectories())
        {
            var path = Path.Combine(directory, "VERSION");
            if (!File.Exists(path))
            {
                continue;
            }

            var version = File.ReadAllText(path).Trim();
            if (IsReleaseVersion(version))
            {
                return version;
            }
        }

        return "v0.8.10";
    }

    private static IEnumerable<string> CandidateDirectories()
    {
        yield return AppContext.BaseDirectory;
        yield return Directory.GetCurrentDirectory();

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static bool IsReleaseVersion(string version)
    {
        if (!version.StartsWith('v') && !version.StartsWith('V'))
        {
            return false;
        }

        var parts = version[1..].Split('.');
        return parts.Length == 3 && parts.All(part => int.TryParse(part, out _));
    }
}
