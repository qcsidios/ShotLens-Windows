using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ShotLens.Windows.Core.Translation;

namespace ShotLens.Windows.Core.Tests.Translation;

public sealed class TranslationServiceTests
{
    [Fact]
    public async Task Default_free_configuration_uses_siliconflow_chat_completions()
    {
        var handler = new FakeHandler(
            """
            {"choices":[{"message":{"content":"[\"你好\",\"世界\"]"}}]}
            """);
        var service = new TranslationService(new HttpClient(handler));

        var result = await service.TranslateAsync(
            ["Hello", "World"],
            "中文",
            ApiConfiguration.Default,
            "sk-default",
            CancellationToken.None);

        Assert.Equal(["你好", "世界"], result.Translations);
        Assert.Equal(
            "https://api.siliconflow.cn/v1/chat/completions",
            handler.RequestUri?.ToString());
        Assert.Equal("Bearer sk-default", handler.Authorization);
        Assert.Equal("tencent/Hunyuan-MT-7B", handler.Model);
    }

    [Fact]
    public async Task Custom_key_overrides_default_key()
    {
        var handler = new FakeHandler(
            """
            {"choices":[{"message":{"content":"[\"译文\"]"}}]}
            """);
        var service = new TranslationService(new HttpClient(handler));
        var configuration = ApiConfiguration.Default with
        {
            Mode = ApiCredentialMode.Custom,
            UserApiKey = "sk-user"
        };

        await service.TranslateAsync(
            ["text"],
            "中文",
            configuration,
            "sk-default",
            CancellationToken.None);

        Assert.Equal("Bearer sk-user", handler.Authorization);
    }

    [Theory]
    [InlineData("```json\n[\"译文\"]\n```")]
    [InlineData("{\"translations\":[\"译文\"]}")]
    [InlineData("{\"result\":[\"译文\"]}")]
    public async Task Response_parser_accepts_common_json_wrappers(
        string content)
    {
        var handler = new FakeHandler(
            JsonSerializer.Serialize(
                new
                {
                    choices = new[]
                    {
                        new
                        {
                            message = new { content }
                        }
                    }
                }));
        var service = new TranslationService(new HttpClient(handler));

        var result = await service.TranslateAsync(
            ["text"],
            "中文",
            ApiConfiguration.Default,
            "sk-default",
            CancellationToken.None);

        Assert.Equal(["译文"], result.Translations);
    }

    [Fact]
    public async Task Translation_count_must_match_source_count()
    {
        var handler = new FakeHandler(
            """
            {"choices":[{"message":{"content":"[\"only one\"]"}}]}
            """);
        var service = new TranslationService(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<TranslationException>(
            () => service.TranslateAsync(
                ["one", "two"],
                "中文",
                ApiConfiguration.Default,
                "sk-default",
                CancellationToken.None));

        Assert.Contains("数量", exception.Message);
    }

    [Fact]
    public async Task Errors_do_not_include_api_keys()
    {
        var handler = new FakeHandler("bad gateway")
        {
            StatusCode = HttpStatusCode.BadGateway
        };
        var service = new TranslationService(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<TranslationException>(
            () => service.TranslateAsync(
                ["text"],
                "中文",
                ApiConfiguration.Default,
                "sk-secret-default",
                CancellationToken.None));

        Assert.DoesNotContain("sk-secret-default", exception.Message);
        Assert.Contains("翻译服务请求失败", exception.Message);
    }

    private sealed class FakeHandler(string response)
        : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public string? Authorization { get; private set; }

        public string? Model { get; private set; }

        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            Model = document.RootElement.GetProperty("model").GetString();
            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(
                    response,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
