using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShotLens.Windows.Core.Ocr.Benchmark;

public static class OcrBenchmarkManifestJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static string Serialize(OcrBenchmarkDatasetManifest manifest) =>
        JsonSerializer.Serialize(manifest, Options);

    public static OcrBenchmarkDatasetManifest Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<OcrBenchmarkDatasetManifest>(
                    json,
                    Options)
                ?? throw new OcrBenchmarkValidationException(
                    "基准集 Manifest 不能为 null。");
        }
        catch (JsonException exception)
        {
            throw new OcrBenchmarkValidationException(
                $"基准集 Manifest JSON 无效：{exception.Message}");
        }
    }
}
