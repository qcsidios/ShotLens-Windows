using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Core.Ocr.Benchmark;

namespace ShotLens.Windows.Ocr.Tests.Benchmark;

public sealed class OcrBenchmarkScorerTests
{
    [Fact]
    public void Exact_result_has_zero_cer_and_full_recall_and_order()
    {
        var result = OcrBenchmarkScorer.Score(
            ["alpha", "beta"],
            [Block("alpha", 0), Block("beta", 1)]);

        Assert.Equal(0, result.EditDistance);
        Assert.Equal(0, result.CharacterErrorRate);
        Assert.Equal(1, result.LineRecall);
        Assert.Equal(1, result.ReadingOrderAccuracy);
        Assert.Empty(result.MissingLines);
    }

    [Theory]
    [InlineData("abc", "abxc")]
    [InlineData("abc", "ac")]
    [InlineData("abc", "axc")]
    public void Counts_one_insertion_deletion_or_substitution(
        string expected,
        string actual)
    {
        var result = OcrBenchmarkScorer.Score(
            [expected],
            [Block(actual, 0)]);

        Assert.Equal(1, result.EditDistance);
        Assert.Equal(1d / 3d, result.CharacterErrorRate, 8);
    }

    [Fact]
    public void Missing_line_reduces_line_recall()
    {
        var result = OcrBenchmarkScorer.Score(
            ["one", "two"],
            [Block("one", 0)]);

        Assert.Equal(3, result.EditDistance);
        Assert.Equal(0.5, result.CharacterErrorRate);
        Assert.Equal(0.5, result.LineRecall);
        Assert.Equal(["two"], result.MissingLines);
    }

    [Theory]
    [InlineData("ShotLens Settings", "Shotlens Settings")]
    [InlineData("API 连接测试", "API连接测试")]
    public void Minor_ocr_or_whitespace_difference_still_recalls_the_line(
        string expected,
        string actual)
    {
        var result = OcrBenchmarkScorer.Score(
            [expected],
            [Block(actual, 0)]);

        Assert.Equal(1, result.LineRecall);
        Assert.Empty(result.MissingLines);
    }

    [Fact]
    public void Swapped_lines_use_pairwise_reading_order_accuracy()
    {
        var result = OcrBenchmarkScorer.Score(
            ["A", "B", "C"],
            [Block("B", 0), Block("A", 1), Block("C", 2)]);

        Assert.Equal(1, result.LineRecall);
        Assert.Equal(2d / 3d, result.ReadingOrderAccuracy, 8);
    }

    private static OcrTextBlock Block(string text, int order) =>
        new(
            text,
            new PhysicalRect(0, order * 20, 100, 20),
            1,
            "en",
            16,
            0.5,
            order);
}
