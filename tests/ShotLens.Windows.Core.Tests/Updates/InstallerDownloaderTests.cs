using System.Net;
using ShotLens.Windows.Core.Updates;
using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Tests.Updates;

public sealed class InstallerDownloaderTests
{
    [Fact]
    public async Task Successful_download_is_atomically_renamed_and_reports_completion()
    {
        var directory = CreateTempDirectory();
        try
        {
            var progress = new List<double>();
            var downloader = new InstallerDownloader(
                new HttpClient(new BytesHandler(HttpStatusCode.OK, [1, 2, 3])),
                directory);

            var path = await downloader.DownloadAsync(
                BetaUpdate(),
                new Progress<double>(value => progress.Add(value)),
                CancellationToken.None);

            Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(path));
            Assert.EndsWith(".exe", path, StringComparison.Ordinal);
            Assert.Empty(Directory.GetFiles(directory, "*.partial"));
            Assert.Contains(1d, progress);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Empty_download_is_rejected_and_partial_file_is_removed()
    {
        var directory = CreateTempDirectory();
        try
        {
            var downloader = new InstallerDownloader(
                new HttpClient(new BytesHandler(HttpStatusCode.OK, [])),
                directory);

            var error = await Assert.ThrowsAsync<UpdateTransportException>(
                () => downloader.DownloadAsync(BetaUpdate(), null, CancellationToken.None));

            Assert.Equal("下载的安装包为空。", error.Message);
            Assert.Empty(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Wrong_asset_name_is_rejected_before_http_request()
    {
        var directory = CreateTempDirectory();
        try
        {
            var handler = new BytesHandler(HttpStatusCode.OK, [1]);
            var downloader = new InstallerDownloader(new HttpClient(handler), directory);
            var update = BetaUpdate() with
            {
                Installer = new GitHubAsset(
                    "wrong.exe",
                    new Uri("https://github.test/wrong.exe"))
            };

            await Assert.ThrowsAsync<UpdateTransportException>(
                () => downloader.DownloadAsync(update, null, CancellationToken.None));

            Assert.Equal(0, handler.RequestCount);
            Assert.Empty(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AvailableUpdate BetaUpdate() =>
        new(
            SemanticVersion.Parse("v0.2.0-beta.2"),
            new Uri("https://github.test/release"),
            new GitHubAsset(
                "ShotLens-Beta-v0.2.0-beta.2-Setup.exe",
                new Uri("https://github.test/setup.exe")));

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ShotLens-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class BytesHandler(HttpStatusCode statusCode, byte[] body) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(body)
            });
        }
    }
}
