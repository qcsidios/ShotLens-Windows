using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using ShotLens.Windows.Core;

namespace ShotLens.Windows.App.Services;

public sealed record UpdateCheckResult(bool HasUpdate, string Message, string? ReleaseUrl);

public sealed class UpdateChecker
{
    private static readonly Uri ReleasesUri = new("https://api.github.com/repos/qcsidios/ShotLens/releases?per_page=30");
    private readonly HttpClient httpClient;

    public UpdateChecker(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
        if (!this.httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ShotLens-Windows");
        }
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var body = await httpClient.GetStringAsync(ReleasesUri, cancellationToken);
        using var document = JsonDocument.Parse(body);
        var release = FindLatestWindowsRelease(document.RootElement);

        if (release is not null && CompareVersions(release.Tag, VersionInfo.Current) > 0)
        {
            return new UpdateCheckResult(true, $"发现新版本 {release.Tag}", release.ReleaseUrl);
        }

        return new UpdateCheckResult(false, $"当前已是最新版本 {VersionInfo.Current}", ReleaseUrl: null);
    }

    public static void OpenReleasePage(string releaseUrl)
    {
        Process.Start(new ProcessStartInfo(releaseUrl)
        {
            UseShellExecute = true
        });
    }

    private static int CompareVersions(string left, string right)
    {
        var leftParts = VersionParts(left);
        var rightParts = VersionParts(right);
        for (var index = 0; index < Math.Max(leftParts.Length, rightParts.Length); index++)
        {
            var leftPart = index < leftParts.Length ? leftParts[index] : 0;
            var rightPart = index < rightParts.Length ? rightParts[index] : 0;
            if (leftPart != rightPart)
            {
                return leftPart.CompareTo(rightPart);
            }
        }

        return 0;
    }

    private static int[] VersionParts(string version) =>
        Regex.Match(version, @"(\d+(?:\.\d+){1,3})").Value.Split('.')
            .Select(part => int.TryParse(part, out var value) ? value : 0)
            .ToArray();

    private static WindowsRelease? FindLatestWindowsRelease(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("GitHub Release 返回格式无效。");
        }

        return root.EnumerateArray()
            .Where(release => !release.TryGetProperty("draft", out var draft) || !draft.GetBoolean())
            .Select(ParseWindowsRelease)
            .Where(release => release is not null)
            .OrderByDescending(release => release!.Tag, Comparer<string>.Create(CompareVersions))
            .FirstOrDefault();
    }

    private static WindowsRelease? ParseWindowsRelease(JsonElement release)
    {
        var tag = release.TryGetProperty("tag_name", out var tagName) ? tagName.GetString() ?? "" : "";
        var releaseUrl = release.TryGetProperty("html_url", out var htmlUrl) ? htmlUrl.GetString() : null;
        if (VersionParts(tag).Length < 2 || string.IsNullOrWhiteSpace(releaseUrl))
        {
            return null;
        }

        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var hasInstaller = assets.EnumerateArray().Any(asset =>
        {
            var name = asset.TryGetProperty("name", out var assetName) ? assetName.GetString() ?? "" : "";
            return name.StartsWith("ShotLens-Windows-", StringComparison.Ordinal)
                && name.EndsWith("-Setup.exe", StringComparison.Ordinal)
                && name.Contains(tag, StringComparison.Ordinal);
        });

        return hasInstaller ? new WindowsRelease(tag, releaseUrl) : null;
    }

    private sealed record WindowsRelease(string Tag, string ReleaseUrl);
}
