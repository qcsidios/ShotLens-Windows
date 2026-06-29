using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr.Benchmark;

namespace ShotLens.Windows.Ocr.Tests.Benchmark;

public sealed class OcrBenchmarkManifestJsonTests
{
    [Fact]
    public void Round_trips_machine_readable_manifest_with_string_enums()
    {
        var manifest = new OcrBenchmarkDatasetManifest(
            "1",
            20260629,
            "WPF",
            new OcrBenchmarkRenderSettings(
                "Segoe UI",
                "Microsoft YaHei UI",
                new PhysicalSize(960, 540),
                [96]),
            [
                new OcrBenchmarkSample(
                    "en-001",
                    "en-001.png",
                    OcrBenchmarkLanguage.English,
                    OcrBenchmarkTheme.Dark,
                    OcrBenchmarkLayout.Menu,
                    96,
                    new PhysicalSize(960, 540),
                    16,
                    [new OcrBenchmarkExpectedLine("Settings", 0)],
                    new string('a', 64),
                    false)
            ]);

        var json = OcrBenchmarkManifestJson.Serialize(manifest);
        var result = OcrBenchmarkManifestJson.Deserialize(json);

        Assert.Contains("\"language\": \"english\"", json);
        Assert.Contains("\"theme\": \"dark\"", json);
        Assert.Equal(manifest.RandomSeed, result.RandomSeed);
        Assert.Equal(manifest.Samples[0].Id, result.Samples[0].Id);
        Assert.Equal(manifest.Samples[0].Language, result.Samples[0].Language);
        Assert.Equal(
            manifest.Samples[0].ExpectedLines,
            result.Samples[0].ExpectedLines);
    }
}
