using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ShotLens.Windows.Core.Translation;

public sealed class TranslationService(HttpClient httpClient)
{
    public async Task<TranslationResult> TranslateAsync(
        IReadOnlyList<string> sourceTexts,
        string targetLanguage,
        ApiConfiguration configuration,
        string? defaultApiKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceTexts);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguage);
        ArgumentNullException.ThrowIfNull(configuration);
        if (sourceTexts.Count == 0)
        {
            return new TranslationResult([]);
        }

        var apiKey = ResolveApiKey(configuration, defaultApiKey);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            ChatCompletionsUri(configuration.BaseUrl));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    model = configuration.Model,
                    temperature = 0,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "你是 ShotLens 的翻译引擎。只输出 JSON 字符串数组，数组长度必须与输入 texts 完全一致，不要解释。"
                        },
                        new
                        {
                            role = "user",
                            content = JsonSerializer.Serialize(
                                new
                                {
                                    targetLanguage,
                                    texts = sourceTexts
                                })
                        }
                    }
                }),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await httpClient.SendAsync(
                request,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new TranslationException("翻译服务请求失败。");
            }

            var json = await response.Content.ReadAsStringAsync(
                cancellationToken);
            var translations = TranslationJson.ParseChatCompletions(
                json,
                sourceTexts.Count);
            return new TranslationResult(translations);
        }
        catch (TranslationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new TranslationException(
                "翻译服务请求失败。",
                exception);
        }
    }

    private static string ResolveApiKey(
        ApiConfiguration configuration,
        string? defaultApiKey)
    {
        if (configuration.Mode == ApiCredentialMode.Disabled)
        {
            throw new TranslationException("翻译 API 已禁用。");
        }

        var apiKey = configuration.Mode == ApiCredentialMode.Custom
            ? configuration.UserApiKey
            : defaultApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new TranslationException("请先填写 API Key。");
        }

        return apiKey;
    }

    private static Uri ChatCompletionsUri(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        return new Uri(
            $"{baseUrl.TrimEnd('/')}/chat/completions",
            UriKind.Absolute);
    }
}
