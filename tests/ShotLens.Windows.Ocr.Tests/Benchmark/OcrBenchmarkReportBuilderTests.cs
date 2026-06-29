using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Core.Ocr.Benchmark;

namespace ShotLens.Windows.Ocr.Tests.Benchmark;

public sealed class OcrBenchmarkReportBuilderTests
{
    [Fact]
    public void Aggregates_weighted_accuracy_timings_memory_and_missed_paragraphs()
    {
        var manifest = new OcrBenchmarkDatasetManifest(
            "1",
            1,
            "test",
            new OcrBenchmarkRenderSettings(
                "Segoe UI",
                "Microsoft YaHei UI",
                new PhysicalSize(100, 50),
                [96]),
            [
                Sample("one", ["abc"]),
                Sample("two", ["xy"])
            ]);
        var machine = new OcrBenchmarkMachine(
            "Windows",
            "CPU",
            1000,
            "GPU",
            "平衡",
            "100%");

        var report = OcrBenchmarkReportBuilder.Build(
            manifest,
            "onnx-paddleocr",
            "engine-1",
            "model-1",
            new string('a', 64),
            [
                new OcrBenchmarkSampleRun(
                    "one",
                    [
                        new OcrTextBlock(
                            "abc",
                            new PhysicalRect(0, 0, 3, 1),
                            1,
                            "en",
                            1,
                            1,
                            0)
                    ],
                    5,
                    10,
                    100),
                new OcrBenchmarkSampleRun(
                    "two",
                    [],
                    15,
                    30,
                    200)
            ],
            DateTimeOffset.Parse("2026-06-29T00:00:00Z"),
            startupMilliseconds: 40,
            publishedSizeBytes: 300,
            modelSizeBytes: 150,
            machine);

        Assert.Equal(0.6, report.CharacterAccuracy, precision: 6);
        Assert.Equal(0.5, report.LineRecall, precision: 6);
        Assert.Equal(1, report.ReadingOrderAccuracy);
        Assert.Equal(10, report.MedianOcrMilliseconds);
        Assert.Equal(15, report.P95OcrMilliseconds);
        Assert.Equal(20, report.MedianTotalMilliseconds);
        Assert.Equal(200, report.PeakMemoryBytes);
        Assert.Equal(["two"], report.MissedParagraphSampleIds);
        var language = Assert.Single(report.LanguageMetrics);
        Assert.Equal(OcrBenchmarkLanguage.English, language.Language);
        Assert.Equal(2, language.SampleCount);
        Assert.Equal(0.6, language.CharacterAccuracy, precision: 6);
    }

    [Fact]
    public void Rejects_missing_or_duplicate_sample_runs()
    {
        var manifest = new OcrBenchmarkDatasetManifest(
            "1",
            1,
            "test",
            new OcrBenchmarkRenderSettings(
                "Segoe UI",
                "Microsoft YaHei UI",
                new PhysicalSize(100, 50),
                [96]),
            [Sample("one", ["abc"]), Sample("two", ["xy"])]);
        var duplicateRuns = new[]
        {
            new OcrBenchmarkSampleRun("one", [], 1, 1, 1),
            new OcrBenchmarkSampleRun("one", [], 1, 1, 1)
        };

        Assert.Throws<ArgumentException>(
            () => OcrBenchmarkReportBuilder.Build(
                manifest,
                "engine",
                "version",
                "model",
                new string('a', 64),
                duplicateRuns,
                DateTimeOffset.UtcNow,
                1,
                1,
                1,
                new OcrBenchmarkMachine("w", "c", 1, "g", "p", "d")));
    }

    [Fact]
    public void Inexact_nonempty_paragraph_is_not_a_whole_paragraph_miss()
    {
        var manifest = new OcrBenchmarkDatasetManifest(
            "1",
            1,
            "test",
            new OcrBenchmarkRenderSettings(
                "Segoe UI",
                "Microsoft YaHei UI",
                new PhysicalSize(100, 50),
                [96]),
            [Sample("one", ["abc"])]);
        var report = OcrBenchmarkReportBuilder.Build(
            manifest,
            "engine",
            "version",
            "model",
            new string('a', 64),
            [
                new OcrBenchmarkSampleRun(
                    "one",
                    [
                        new OcrTextBlock(
                            "axc",
                            new PhysicalRect(0, 0, 3, 1),
                            1,
                            "en",
                            1,
                            1,
                            0)
                    ],
                    1,
                    1,
                    1)
            ],
            DateTimeOffset.UtcNow,
            1,
            1,
            1,
            new OcrBenchmarkMachine("w", "c", 1, "g", "p", "d"));

        Assert.Empty(report.MissedParagraphSampleIds);
    }

    private static OcrBenchmarkSample Sample(
        string id,
        string[] lines) =>
        new(
            id,
            $"{id}.png",
            OcrBenchmarkLanguage.English,
            OcrBenchmarkTheme.Light,
            OcrBenchmarkLayout.Title,
            96,
            new PhysicalSize(100, 50),
            12,
            lines.Select(
                    (text, order) =>
                        new OcrBenchmarkExpectedLine(text, order))
                .ToArray(),
            new string('a', 64),
            false);
}
