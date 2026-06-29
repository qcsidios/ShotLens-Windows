using System.Text.Json;
using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Updates;

public sealed class GitHubReleaseSource(HttpClient httpClient) : IGitHubReleaseSource
{
    private const int MaxResponseBytes = 2 * 1024 * 1024;
    private static readonly Uri ReleasesUri = new(
        "https://api.github.com/repos/qcsidios/ShotLens-Windows/releases?per_page=30");

    public async Task<IReadOnlyList<GitHubRelease>> GetReleasesAsync(
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(10));
        var requestToken = timeoutSource.Token;
        using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesUri);
        request.Headers.UserAgent.ParseAdd("ShotLens-Windows");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                requestToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UpdateTransportException("连接 GitHub 更新服务超时。");
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateTransportException("无法连接 GitHub 更新服务。", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new UpdateTransportException("无法连接 GitHub 更新服务。");
            }

            if (response.Content.Headers.ContentLength is > MaxResponseBytes)
            {
                throw new UpdateTransportException("GitHub Release 返回内容过大。");
            }

            try
            {
                await using var stream = await ReadBoundedContentAsync(
                    response.Content,
                    requestToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: requestToken);
                return ParseReleases(document.RootElement);
            }
            catch (JsonException ex)
            {
                throw new UpdateTransportException("GitHub Release 返回格式无效。", ex);
            }
        }
    }

    private static async Task<MemoryStream> ReadBoundedContentAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        var target = new MemoryStream();
        var buffer = new byte[64 * 1024];

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                target.Position = 0;
                return target;
            }

            if (target.Length + read > MaxResponseBytes)
            {
                target.Dispose();
                throw new UpdateTransportException("GitHub Release 返回内容过大。");
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static IReadOnlyList<GitHubRelease> ParseReleases(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException();
        }

        var releases = new List<GitHubRelease>();
        foreach (var element in root.EnumerateArray())
        {
            if (!TryParseRelease(element, out var release))
            {
                continue;
            }

            releases.Add(release);
        }

        return releases;
    }

    private static bool TryParseRelease(JsonElement element, out GitHubRelease release)
    {
        release = null!;
        if (!TryGetString(element, "tag_name", out var tag)
            || !TryGetString(element, "html_url", out var releasePage)
            || !Uri.TryCreate(releasePage, UriKind.Absolute, out var releaseUri))
        {
            return false;
        }

        SemanticVersion version;
        try
        {
            version = SemanticVersion.Parse(tag);
        }
        catch (FormatException)
        {
            return false;
        }

        var assets = new List<GitHubAsset>();
        if (element.TryGetProperty("assets", out var assetElements)
            && assetElements.ValueKind == JsonValueKind.Array)
        {
            foreach (var assetElement in assetElements.EnumerateArray())
            {
                if (TryGetString(assetElement, "name", out var name)
                    && TryGetString(assetElement, "browser_download_url", out var download)
                    && Uri.TryCreate(download, UriKind.Absolute, out var downloadUri))
                {
                    assets.Add(new GitHubAsset(name, downloadUri));
                }
            }
        }

        release = new GitHubRelease(
            version,
            GetBoolean(element, "draft"),
            GetBoolean(element, "prerelease"),
            releaseUri,
            assets);
        return true;
    }

    private static bool TryGetString(
        JsonElement element,
        string propertyName,
        out string value)
    {
        value = "";
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value = property.GetString() ?? "");
    }

    private static bool GetBoolean(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
        && property.ValueKind is JsonValueKind.True or JsonValueKind.False
        && property.GetBoolean();
}

public sealed class UpdateTransportException : Exception
{
    public UpdateTransportException(string message)
        : base(message)
    {
    }

    public UpdateTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
