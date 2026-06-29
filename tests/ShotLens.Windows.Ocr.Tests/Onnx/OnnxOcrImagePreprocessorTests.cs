using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Ocr.Worker.Onnx;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Tests.Onnx;

public sealed class OnnxOcrImagePreprocessorTests
{
    [Theory]
    [InlineData(SKColorType.Bgra8888)]
    [InlineData(SKColorType.Rgb888x)]
    public void Bgra_and_rgb_images_are_normalized_for_onnx(
        SKColorType colorType)
    {
        using var source = new SKBitmap(
            new SKImageInfo(
                4,
                2,
                colorType,
                SKAlphaType.Premul));
        source.Erase(SKColors.CornflowerBlue);

        using var prepared = OnnxOcrImagePreprocessor.Prepare(
            source,
            minimumShortSide: 2,
            maximumLongSide: 4);

        Assert.Equal(SKColorType.Bgra8888, prepared.Bitmap.ColorType);
        Assert.Equal(new SKSizeI(4, 2), prepared.Bitmap.Info.Size);
        Assert.Equal(SKColors.CornflowerBlue, prepared.Bitmap.GetPixel(1, 1));
    }

    [Theory]
    [InlineData(1200, 800, 1200, 800, 1)]
    [InlineData(1000, 500, 1472, 736, 1.472)]
    [InlineData(4000, 2000, 2000, 1000, 0.5)]
    public void Resize_is_proportional_and_bounded(
        int width,
        int height,
        int expectedWidth,
        int expectedHeight,
        double expectedScale)
    {
        using var source = new SKBitmap(width, height);

        using var prepared = OnnxOcrImagePreprocessor.Prepare(
            source,
            minimumShortSide: 736,
            maximumLongSide: 2000);

        Assert.Equal(expectedWidth, prepared.Bitmap.Width);
        Assert.Equal(expectedHeight, prepared.Bitmap.Height);
        Assert.Equal(expectedScale, prepared.Scale, precision: 3);
        Assert.Equal(
            (double)prepared.Bitmap.Width / width,
            (double)prepared.Bitmap.Height / height,
            precision: 3);
    }

    [Fact]
    public void Small_text_image_is_upscaled_before_ocr()
    {
        using var source = new SKBitmap(800, 400);

        using var prepared = OnnxOcrImagePreprocessor.Prepare(
            source,
            minimumShortSide: 800,
            maximumLongSide: 2000);

        Assert.Equal(2, prepared.Scale);
        Assert.Equal(new SKSizeI(1600, 800), prepared.Bitmap.Info.Size);
    }

    [Fact]
    public void Coordinates_are_restored_to_source_pixels()
    {
        var bounds = OnnxOcrCoordinateMapper.RestoreBounds(
            [
                new SKPointI(200, 100),
                new SKPointI(600, 100),
                new SKPointI(600, 300),
                new SKPointI(200, 300)
            ],
            scale: 2,
            new PhysicalSize(800, 400));

        Assert.Equal(new PhysicalRect(100, 50, 200, 100), bounds);
    }

    [Fact]
    public void Restored_bounds_are_clamped_to_the_source_image()
    {
        var bounds = OnnxOcrCoordinateMapper.RestoreBounds(
            [
                new SKPointI(-20, -10),
                new SKPointI(1700, -10),
                new SKPointI(1700, 900),
                new SKPointI(-20, 900)
            ],
            scale: 2,
            new PhysicalSize(800, 400));

        Assert.Equal(new PhysicalRect(0, 0, 800, 400), bounds);
    }
}
