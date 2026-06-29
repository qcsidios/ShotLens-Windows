using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Core.Ocr.Benchmark;

public sealed record OcrBenchmarkDatasetManifest(
    string GeneratorVersion,
    int RandomSeed,
    string Renderer,
    OcrBenchmarkRenderSettings RenderSettings,
    OcrBenchmarkSample[] Samples);

public sealed record OcrBenchmarkRenderSettings(
    string EnglishFont,
    string ChineseFont,
    PhysicalSize LogicalViewport,
    int[] DpiValues);

public sealed record OcrBenchmarkSample(
    string Id,
    string ImageFile,
    OcrBenchmarkLanguage Language,
    OcrBenchmarkTheme Theme,
    OcrBenchmarkLayout Layout,
    int Dpi,
    PhysicalSize ViewportPixels,
    double MinimumTextHeightPixels,
    OcrBenchmarkExpectedLine[] ExpectedLines,
    string ImageSha256,
    bool ContainsSensitiveData);

public sealed record OcrBenchmarkExpectedLine(string Text, int Order);

public enum OcrBenchmarkLanguage
{
    English,
    Chinese,
    Mixed
}

public enum OcrBenchmarkTheme
{
    Light,
    Dark
}

public enum OcrBenchmarkLayout
{
    SmallText,
    Title,
    Menu,
    DenseParagraph,
    Mixed
}
