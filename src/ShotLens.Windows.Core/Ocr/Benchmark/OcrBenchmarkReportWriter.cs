using System.Globalization;
using System.Text.Json;

namespace ShotLens.Windows.Core.Ocr.Benchmark;

public static class OcrBenchmarkReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void Write(
        string outputDirectory,
        OcrBenchmarkReport report)
    {
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(
            Path.Combine(outputDirectory, "report.json"),
            JsonSerializer.Serialize(report, JsonOptions));
        File.WriteAllText(
            Path.Combine(outputDirectory, "报告.md"),
            Markdown(report));
    }

    private static string Markdown(OcrBenchmarkReport report)
    {
        var missedParagraphs = report.MissedParagraphSampleIds.Length == 0
            ? "无"
            : string.Join("、", report.MissedParagraphSampleIds);
        return $"""
            # ShotLens OCR 基准报告

            > 本报告使用生成基准，不代表完整真实用户场景。

            ## 基本信息

            - 引擎：`{report.Engine}`
            - 引擎版本：`{report.EngineVersion}`
            - 模型版本：`{report.ModelVersion}`
            - 样本数：{report.SampleCount}
            - 数据集 SHA-256：`{report.DatasetSha256}`

            ## 准确率

            - 字符准确率：{Percent(report.CharacterAccuracy)}
            - 行召回率：{Percent(report.LineRecall)}
            - 阅读顺序准确率：{Percent(report.ReadingOrderAccuracy)}
            - 整段漏识别样本：{missedParagraphs}

            ## 性能与体积

            - 启动耗时：{report.StartupMilliseconds:F1} ms
            - OCR P50：{report.MedianOcrMilliseconds:F1} ms
            - OCR P95：{report.P95OcrMilliseconds:F1} ms
            - 总耗时 P50：{report.MedianTotalMilliseconds:F1} ms
            - 峰值内存：{Mebibytes(report.PeakMemoryBytes):F1} MiB
            - Worker 发布体积：{Mebibytes(report.PublishedSizeBytes):F1} MiB
            - 模型体积：{Mebibytes(report.ModelSizeBytes):F1} MiB

            ## 测试机器

            - Windows：{report.Machine.WindowsVersion}
            - CPU：{report.Machine.Cpu}
            - 内存：{Mebibytes(report.Machine.MemoryBytes) / 1024:F1} GiB
            - GPU：{report.Machine.Gpu}
            - 电源模式：{report.Machine.PowerMode}
            - DPI：{report.Machine.DpiConfiguration}
            """;
    }

    private static string Percent(double value) =>
        value.ToString("P2", CultureInfo.InvariantCulture);

    private static double Mebibytes(long bytes) =>
        bytes / 1024d / 1024d;
}
