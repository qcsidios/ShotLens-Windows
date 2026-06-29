using System.Net;
using System.Text;
using ShotLens.Windows.Core.Updates;
using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Tests.Updates;

public sealed class GitHubReleaseSourceTests
{
    [Fact]
    public async Task Requests_expected_endpoint_with_user_agent_and_maps_release()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """
            [{
              "tag_name": "v0.2.0-beta.2",
              "draft": false,
              "prerelease": true,
              "html_url": "https://github.test/release",
              "assets": [{
                "name": "ShotLens-Beta-v0.2.0-beta.2-Setup.exe",
                "browser_download_url": "https://github.test/setup.exe"
              }]
            }]
            """);
        var source = new GitHubReleaseSource(new HttpClient(handler));

        var releases = await source.GetReleasesAsync(CancellationToken.None);

        Assert.Equal(
            "https://api.github.com/repos/qcsidios/ShotLens-Windows/releases?per_page=30",
            handler.RequestUri?.ToString());
        Assert.Contains("ShotLens-Windows", handler.UserAgent);
        var release = Assert.Single(releases);
        Assert.Equal(SemanticVersion.Parse("v0.2.0-beta.2"), release.Version);
        Assert.True(release.Prerelease);
        Assert.Equal(
            "ShotLens-Beta-v0.2.0-beta.2-Setup.exe",
            Assert.Single(release.Assets).Name);
    }

    [Fact]
    public async Task Http_failure_returns_safe_error_without_response_body()
    {
        const string sensitiveBody = "server-secret-detail";
        var source = new GitHubReleaseSource(
            new HttpClient(new StubHandler(HttpStatusCode.InternalServerError, sensitiveBody)));

        var error = await Assert.ThrowsAsync<UpdateTransportException>(
            () => source.GetReleasesAsync(CancellationToken.None));

        Assert.DoesNotContain(sensitiveBody, error.Message);
        Assert.Contains("GitHub", error.Message);
    }

    [Fact]
    public async Task Invalid_json_returns_safe_error()
    {
        var source = new GitHubReleaseSource(
            new HttpClient(new StubHandler(HttpStatusCode.OK, "not-json")));

        var error = await Assert.ThrowsAsync<UpdateTransportException>(
            () => source.GetReleasesAsync(CancellationToken.None));

        Assert.Equal("GitHub Release 返回格式无效。", error.Message);
    }

    [Fact]
    public async Task Response_larger_than_two_mebibytes_is_rejected()
    {
        var source = new GitHubReleaseSource(
            new HttpClient(new StubHandler(
                HttpStatusCode.OK,
                new string('x', (2 * 1024 * 1024) + 1))));

        var error = await Assert.ThrowsAsync<UpdateTransportException>(
            () => source.GetReleasesAsync(CancellationToken.None));

        Assert.Equal("GitHub Release 返回内容过大。", error.Message);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public string UserAgent { get; private set; } = "";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            UserAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
