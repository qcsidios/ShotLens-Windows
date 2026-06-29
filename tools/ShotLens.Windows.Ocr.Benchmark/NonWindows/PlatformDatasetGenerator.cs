namespace ShotLens.Windows.Ocr.Benchmark;

internal static class PlatformDatasetGenerator
{
    public static void Generate(string outputDirectory) =>
        throw new PlatformNotSupportedException(
            "OCR 基准图片必须在 Windows 上使用 WPF 生成。");
}
