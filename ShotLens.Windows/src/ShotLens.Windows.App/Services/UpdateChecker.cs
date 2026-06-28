using System.Diagnostics;
using System.Text.Json;
using ShotLens.Windows.Core;

namespace ShotLens.Windows.App.Services;

public sealed record UpdateCheckResult(bool HasUpdate, string Message, string? ReleaseUrl);

public sealed class UpdateChecker
{
    private static readonly Uri LatestReleaseUri = new("https://api.github.com/repos/qcsidios/ShotLens/releases/latest");
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
        var body = await httpClient.GetStringAsync(LatestReleaseUri, cancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var tag = root.TryGetProperty("tag_name", out var tagName) ? tagName.GetString() ?? "" : "";
        var releaseUrl = root.TryGetProperty("html_url", out var htmlUrl) ? htmlUrl.GetString() : null;

        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new InvalidOperationException("GitHub Release 返回格式无效。");
        }

        if (CompareVersions(tag, VersionInfo.Current) > 0)
        {
            return new UpdateCheckResult(true, $"发现新版本 {tag}", releaseUrl);
        }

        return new UpdateCheckResult(false, $"当前已是最新版本 {VersionInfo.Current}", releaseUrl);
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
        version.Trim().TrimStart('v', 'V').Split('.')
            .Select(part => int.TryParse(part, out var value) ? value : 0)
            .ToArray();
}
