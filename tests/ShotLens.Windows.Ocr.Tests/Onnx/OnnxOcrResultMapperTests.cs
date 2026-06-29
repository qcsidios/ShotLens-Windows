using RapidOcrNet;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Ocr.Worker.Onnx;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Tests.Onnx;

public sealed class OnnxOcrResultMapperTests
{
    [Fact]
    public void Raw_rapidocr_block_maps_to_versioned_worker_fields()
    {
        using var source = new SKBitmap(100, 50);
        source.Erase(SKColors.White);
        var rapidBlock = Block(
            "中文 ShotLens",
            [
                new SKPointI(20, 10),
                new SKPointI(80, 10),
                new SKPointI(80, 30),
                new SKPointI(20, 30)
            ],
            [0.8f, 0.6f]);

        var result = OnnxOcrResultMapper.Map(
            [rapidBlock],
            source,
            scale: 2);

        var block = Assert.Single(result);
        Assert.Equal("中文 ShotLens", block.Text);
        Assert.Equal(new PhysicalRect(10, 5, 30, 10), block.Bounds);
        Assert.Equal(0.7, block.Confidence, precision: 6);
        Assert.Equal("zh-en", block.Language);
        Assert.Equal(10, block.FontSizePixels);
        Assert.Equal(1, block.Brightness);
        Assert.Equal(0, block.Order);
    }

    [Theory]
    [InlineData("Settings", "en")]
    [InlineData("截图翻译", "zh")]
    [InlineData("123-+-", "und")]
    public void Language_is_inferred_from_recognized_text(
        string text,
        string expectedLanguage)
    {
        using var source = new SKBitmap(40, 20);
        var result = OnnxOcrResultMapper.Map(
            [
                Block(
                    text,
                    [
                        new SKPointI(0, 0),
                        new SKPointI(20, 0),
                        new SKPointI(20, 10),
                        new SKPointI(0, 10)
                    ],
                    [0.9f])
            ],
            source,
            scale: 1);

        Assert.Equal(expectedLanguage, result[0].Language);
    }

    private static TextBlock Block(
        string text,
        SKPointI[] points,
        float[] scores) =>
        new()
        {
            Text = text,
            BoxPoints = points,
            BoxScore = 0.5f,
            Chars = text.Select(character => character.ToString()).ToArray(),
            CharScores = scores
        };
}
