using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using ShotLens.Windows.Core;

namespace ShotLens.Windows.App.Services;

public sealed record UpdateCheckResult(
    bool HasUpdate,
    string Message,
    string? Version,
    string? ReleaseUrl,
    string? InstallerUrl);

public sealed class UpdateChecker
{
    private static readonly Uri ReleasesUri = new("https://api.github.com/repos/qcsidios/ShotLens-Windows/releases?per_page=30");
    private static readonly Uri LatestReleaseUri = new("https://github.com/qcsidios/ShotLens-Windows/releases/latest");
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
        var release = await FindReleaseAsync(cancellationToken);

        if (release is not null && CompareVersions(release.Tag, VersionInfo.Current) > 0)
        {
            return new UpdateCheckResult(
                true,
                $"发现新版本 {release.Tag}",
                release.Tag,
                release.ReleaseUrl,
                release.InstallerUrl);
        }

        return new UpdateCheckResult(
            false,
            $"当前已是最新版本 {VersionInfo.Current}",
            Version: null,
            ReleaseUrl: null,
            InstallerUrl: null);
    }

    private async Task<WindowsRelease?> FindReleaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var body = await httpClient.GetStringAsync(ReleasesUri, cancellationToken);
            using var document = JsonDocument.Parse(body);
            var apiRelease = FindLatestWindowsRelease(document.RootElement);
            if (apiRelease is not null)
            {
                return apiRelease;
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException or InvalidOperationException)
        {
        }

        return await FindReleaseFromLatestPageAsync(cancellationToken);
    }

    private async Task<WindowsRelease> FindReleaseFromLatestPageAsync(CancellationToken cancellationToken)
    {
        using var releaseResponse = await httpClient.GetAsync(
            LatestReleaseUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        releaseResponse.EnsureSuccessStatusCode();

        var releaseUri = releaseResponse.RequestMessage?.RequestUri;
        var tag = releaseUri is null
            ? ""
            : Regex.Match(releaseUri.AbsolutePath, @"/releases/tag/([^/]+)/*$").Groups[1].Value;
        tag = Uri.UnescapeDataString(tag);
        if (VersionParts(tag).Length < 2)
        {
            throw new InvalidOperationException("无法从 GitHub Release 页面识别最新版本。");
        }

        var installerName = $"ShotLens-Windows-{tag}-Setup.exe";
        var installerUrl = $"https://github.com/qcsidios/ShotLens-Windows/releases/download/{Uri.EscapeDataString(tag)}/{installerName}";
        using var installerResponse = await httpClient.GetAsync(
            installerUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        installerResponse.EnsureSuccessStatusCode();

        return new WindowsRelease(tag, releaseUri!.ToString(), installerUrl);
    }

    public async Task<string> DownloadInstallerAsync(
        UpdateCheckResult update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(update.InstallerUrl) || string.IsNullOrWhiteSpace(update.Version))
        {
            throw new InvalidOperationException("没有可下载的 Windows 安装包。");
        }

        var destination = Path.Combine(
            Path.GetTempPath(),
            $"ShotLens-Windows-{update.Version}-Setup.exe");
        using var response = await httpClient.GetAsync(
            update.InstallerUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(destination);
        var buffer = new byte[1024 * 128];
        long readTotal = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            readTotal += read;
            if (total is > 0)
            {
                progress?.Report((double)readTotal / total.Value);
            }
        }

        progress?.Report(1);
        return destination;
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

        string? installerUrl = null;
        var hasInstaller = assets.EnumerateArray().Any(asset =>
        {
            var name = asset.TryGetProperty("name", out var assetName) ? assetName.GetString() ?? "" : "";
            var isInstaller = name.StartsWith("ShotLens-Windows-", StringComparison.Ordinal)
                && name.EndsWith("-Setup.exe", StringComparison.Ordinal)
                && name.Contains(tag, StringComparison.Ordinal);
            if (isInstaller && asset.TryGetProperty("browser_download_url", out var downloadUrl))
            {
                installerUrl = downloadUrl.GetString();
            }
            return isInstaller;
        });

        return hasInstaller && !string.IsNullOrWhiteSpace(installerUrl)
            ? new WindowsRelease(tag, releaseUrl, installerUrl!)
            : null;
    }

    private sealed record WindowsRelease(string Tag, string ReleaseUrl, string InstallerUrl);
}
