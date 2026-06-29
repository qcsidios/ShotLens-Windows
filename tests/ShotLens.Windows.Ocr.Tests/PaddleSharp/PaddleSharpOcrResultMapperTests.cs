using OpenCvSharp;
using Sdcb.PaddleOCR;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Ocr.Worker.PaddleSharp;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Tests.PaddleSharp;

public sealed class PaddleSharpOcrResultMapperTests
{
    [Fact]
    public void Paddle_region_maps_to_worker_text_block()
    {
        using var source = new SKBitmap(200, 100);
        source.Erase(SKColors.White);
        var region = new PaddleOcrResultRegion(
            new RotatedRect(
                new Point2f(50, 30),
                new Size2f(80, 20),
                angle: 0),
            "截图 Translate",
            0.85f,
            [
                new RecognizedChar("截", 0.9f, 1),
                new RecognizedChar("图", 0.8f, 2)
            ]);

        var blocks = PaddleSharpOcrResultMapper.Map([region], source);

        var block = Assert.Single(blocks);
        Assert.Equal("截图 Translate", block.Text);
        Assert.Equal(new PhysicalRect(10, 20, 80, 20), block.Bounds);
        Assert.Equal(0.85, block.Confidence, precision: 6);
        Assert.Equal("zh-en", block.Language);
        Assert.Equal(20, block.FontSizePixels);
        Assert.Equal(1, block.Brightness);
        Assert.Equal(0, block.Order);
    }

    [Fact]
    public void Rotated_region_is_clamped_to_source_pixels()
    {
        using var source = new SKBitmap(100, 50);
        var region = new PaddleOcrResultRegion(
            new RotatedRect(
                new Point2f(0, 0),
                new Size2f(80, 30),
                angle: 20),
            "Settings",
            0.8f,
            []);

        var block = Assert.Single(
            PaddleSharpOcrResultMapper.Map([region], source));

        Assert.InRange(block.Bounds.X, 0, 99);
        Assert.InRange(block.Bounds.Y, 0, 49);
        Assert.InRange(block.Bounds.Right, 1, 100);
        Assert.InRange(block.Bounds.Bottom, 1, 50);
    }
}
