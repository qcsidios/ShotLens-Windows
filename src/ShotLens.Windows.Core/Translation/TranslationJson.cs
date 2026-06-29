using System.Text.Json;

namespace ShotLens.Windows.Core.Translation;

public static class TranslationJson
{
    public static string[] ParseChatCompletions(
        string responseJson,
        int expectedCount)
    {
        try
        {
            using var response = JsonDocument.Parse(responseJson);
            var content = response.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new TranslationException("翻译服务返回为空。");
            }

            var translations = ParseContent(content);
            if (translations.Length != expectedCount)
            {
                throw new TranslationException("翻译结果数量与原文数量不一致。");
            }

            return translations;
        }
        catch (TranslationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new TranslationException(
                "翻译服务返回格式无效。",
                exception);
        }
    }

    private static string[] ParseContent(string content)
    {
        var json = StripCodeFence(content.Trim());
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            return ReadStringArray(root);
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in new[] { "translations", "result", "items" })
            {
                if (root.TryGetProperty(property, out var array)
                    && array.ValueKind == JsonValueKind.Array)
                {
                    return ReadStringArray(array);
                }
            }
        }

        throw new TranslationException("翻译服务返回格式无效。");
    }

    private static string StripCodeFence(string content)
    {
        if (!content.StartsWith("```", StringComparison.Ordinal))
        {
            return content;
        }

        var firstLineEnd = content.IndexOf('\n', StringComparison.Ordinal);
        var lastFence = content.LastIndexOf("```", StringComparison.Ordinal);
        if (firstLineEnd < 0 || lastFence <= firstLineEnd)
        {
            return content;
        }

        return content[(firstLineEnd + 1)..lastFence].Trim();
    }

    private static string[] ReadStringArray(JsonElement array) =>
        array.EnumerateArray()
            .Select(element => element.GetString() ?? string.Empty)
            .ToArray();
}
