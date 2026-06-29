using System.Net;
using System.Net.Http;
using System.Text;
using ShotLens.Windows.Core;

await VersionInfoTests.ReadsSharedRootVersion();
await TranslationSettingsTests.NormalizesCommonOpenAIEndpointForms();
await TranslationSettingsTests.UsesDefaultFallbackWhenSavedFieldsAreEmpty();
await OpenAITranslatorTests.ParsesCommonModelResponseShapes();
await OpenAITranslatorTests.SendsChatCompletionsRequestToNormalizedEndpoint();

Console.WriteLine("ShotLens.Windows.Core tests passed.");

static class VersionInfoTests
{
    public static Task ReadsSharedRootVersion()
    {
        Assert.Equal("v0.1.3", VersionInfo.Current);
        Assert.Equal("0.1.3", VersionInfo.SemVer);
        return Task.CompletedTask;
    }
}

static class TranslationSettingsTests
{
    public static Task NormalizesCommonOpenAIEndpointForms()
    {
        Assert.Equal(
            "https://example.com/v1/chat/completions",
            new TranslationSettings("https://example.com/v1", "key", "model").ChatCompletionsUri!.ToString());
        Assert.Equal(
            "https://example.com/v1/chat/completions",
            new TranslationSettings("https://example.com/v1/", "key", "model").ChatCompletionsUri!.ToString());
        Assert.Equal(
            "https://example.com/v1/chat/completions",
            new TranslationSettings("https://example.com/v1/chat/completions", "key", "model").ChatCompletionsUri!.ToString());
        Assert.Equal(
            "https://example.com/v1/chat/completions",
            new TranslationSettings("https://example.com/v1/models", "key", "model").ChatCompletionsUri!.ToString());
        return Task.CompletedTask;
    }

    public static Task UsesDefaultFallbackWhenSavedFieldsAreEmpty()
    {
        var settings = new TranslationSettings("", "", "");

        Assert.True(settings.IsConfigured);
        Assert.True(settings.UsesDefaultApiKey);
        Assert.Equal(TranslationSettings.DefaultApiEndpoint, settings.EffectiveApiEndpoint);
        Assert.Equal(TranslationSettings.DefaultModel, settings.EffectiveModel);
        Assert.Equal(TranslationSettings.DefaultApiKey, settings.EffectiveApiKey);
        return Task.CompletedTask;
    }
}

static class OpenAITranslatorTests
{
    public static async Task ParsesCommonModelResponseShapes()
    {
        var cases = new Dictionary<string, string[]>
        {
            ["""["你好","世界"]"""] = ["你好", "世界"],
            ["""{"translations":["你好","世界"]}"""] = ["你好", "世界"],
            ["""{"0":"你好","1":"世界"}"""] = ["你好", "世界"],
            ["""[{"index":0,"translation":"你好"},{"index":1,"translation":"世界"}]"""] = ["你好", "世界"],
            ["```json\n[\"你好\",\"世界\"]\n```"] = ["你好", "世界"],
            ["0: 你好\n1: 世界"] = ["你好", "世界"],
            ["1. 你好\n2. 世界"] = ["你好", "世界"]
        };

        foreach (var (content, expected) in cases)
        {
            var translator = new OpenAITranslator(
                new TranslationSettings("https://shotlens-test.local/v1", "test-key", "test-model"),
                new HttpClient(new StaticOpenAIHandler(content)));

            var actual = await translator.TranslateAsync(["Hello", "World"], "en", "zh-Hans");

            Assert.SequenceEqual(expected, actual);
        }
    }

    public static async Task SendsChatCompletionsRequestToNormalizedEndpoint()
    {
        var handler = new StaticOpenAIHandler("""["你好","世界"]""");
        var translator = new OpenAITranslator(
            new TranslationSettings("https://shotlens-test.local/v1/models", "test-key", "test-model"),
            new HttpClient(handler));

        var actual = await translator.TranslateAsync(["Hello", "World"], "en", "zh-Hans");

        Assert.SequenceEqual(["你好", "世界"], actual);
        Assert.Equal("/v1/chat/completions", handler.RequestedPath);
        Assert.Equal("Bearer test-key", handler.AuthorizationHeader);
        Assert.Contains("test-model", handler.RequestBody);
    }
}

sealed class StaticOpenAIHandler(string assistantContent) : HttpMessageHandler
{
    public string RequestedPath { get; private set; } = "";
    public string AuthorizationHeader { get; private set; } = "";
    public string RequestBody { get; private set; } = "";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedPath = request.RequestUri?.AbsolutePath ?? "";
        AuthorizationHeader = request.Headers.Authorization?.ToString() ?? "";
        RequestBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);

        var escaped = assistantContent
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        var body = $$"""
        {
          "choices": [
            {
              "message": {
                "content": "{{escaped}}"
              }
            }
          ]
        }
        """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}

static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }

    public static void True(bool value)
    {
        if (!value)
        {
            throw new InvalidOperationException("Expected true.");
        }
    }

    public static void Contains(string expectedSubstring, string actual)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{actual}' to contain '{expectedSubstring}'.");
        }
    }

    public static void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException($"Expected {expected.Count} items, got {actual.Count}.");
        }

        for (var index = 0; index < expected.Count; index++)
        {
            Equal(expected[index], actual[index]);
        }
    }
}
