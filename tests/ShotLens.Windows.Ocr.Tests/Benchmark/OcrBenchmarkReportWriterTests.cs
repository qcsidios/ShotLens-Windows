using System.Text.Json;
using ShotLens.Windows.Core.Ocr.Benchmark;

namespace ShotLens.Windows.Ocr.Tests.Benchmark;

public sealed class OcrBenchmarkReportWriterTests : IDisposable
{
    private readonly string _outputDirectory =
        Path.Combine(Path.GetTempPath(), $"shotlens-report-{Guid.NewGuid():N}");

    [Fact]
    public void Writes_machine_json_and_chinese_markdown_with_synthetic_disclaimer()
    {
        var report = new OcrBenchmarkReport(
            1,
            DateTimeOffset.Parse("2026-06-29T00:00:00Z"),
            "fixture",
            "fixture-1",
            "fixture-model-1",
            new string('a', 64),
            60,
            0.98,
            0.96,
            0.95,
            [
                new OcrBenchmarkLanguageMetric(
                    OcrBenchmarkLanguage.English,
                    30,
                    0.99,
                    0.97,
                    1)
            ],
            120,
            40,
            60,
            90,
            256 * 1024 * 1024,
            70 * 1024 * 1024,
            10 * 1024 * 1024,
            new OcrBenchmarkMachine(
                "Windows 11 24H2",
                "x64 CPU",
                16L * 1024 * 1024 * 1024,
                "集成显卡",
                "平衡",
                "100%, 150%"),
            ["mixed-003"]);

        OcrBenchmarkReportWriter.Write(_outputDirectory, report);

        var jsonPath = Path.Combine(_outputDirectory, "report.json");
        var markdownPath = Path.Combine(_outputDirectory, "报告.md");
        using var json = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var markdown = File.ReadAllText(markdownPath);
        Assert.Equal(
            60,
            json.RootElement.GetProperty("sampleCount").GetInt32());
        Assert.Contains("## 准确率", markdown);
        Assert.Contains("## 分语言结果", markdown);
        Assert.Contains("英文（30 张）", markdown);
        Assert.Contains("生成基准，不代表完整真实用户场景", markdown);
        Assert.Contains("mixed-003", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }
}
