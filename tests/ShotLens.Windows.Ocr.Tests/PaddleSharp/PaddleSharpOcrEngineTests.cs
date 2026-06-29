using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Ocr.Worker.PaddleSharp;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Tests.PaddleSharp;

public sealed class PaddleSharpOcrEngineTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"shotlens-paddlesharp-engine-{Guid.NewGuid():N}");

    public PaddleSharpOcrEngineTests() =>
        Directory.CreateDirectory(tempDirectory);

    [Fact]
    public void Model_version_matches_locked_local_v5_package()
    {
        Assert.Equal(
            "Sdcb.PaddleOCR.Models.Local-3.3.1/LocalV5-3.0.0",
            PaddleSharpOcrEngine.ModelVersion);
    }

    [Fact]
    public void Request_size_must_match_decoded_source_pixels()
    {
        var imagePath = CreatePng(100, 50);
        using var engine = new PaddleSharpOcrEngine();
        var request = new OcrProtocolRequest(
            OcrProtocol.CurrentVersion,
            Guid.NewGuid().ToString("N"),
            OcrEngineIds.PaddleSharp,
            imagePath,
            new PhysicalSize(99, 50),
            ["zh", "en"]);

        var exception = Assert.Throws<OcrProtocolException>(
            () => engine.Recognize(request));

        Assert.Contains("尺寸", exception.Message);
    }

    [Fact]
    public void Verified_models_run_real_paddlesharp_inference()
    {
        if (Environment.GetEnvironmentVariable(
                "SHOTLENS_RUN_PADDLE_MODEL_TESTS") != "1")
        {
            return;
        }

        var imagePath = Path.Combine(tempDirectory, "english.png");
        using (var bitmap = new SKBitmap(1000, 300))
        using (var canvas = new SKCanvas(bitmap))
        using (var font = new SKFont(SKTypeface.Default, 64))
        using (var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        })
        {
            canvas.Clear(SKColors.White);
            canvas.DrawText("ShotLens Settings", 60, 170, font, paint);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(imagePath);
            data.SaveTo(stream);
        }

        using var engine = new PaddleSharpOcrEngine();
        var response = engine.Recognize(
            Request(imagePath, new PhysicalSize(1000, 300)));
        var startupMilliseconds = engine.StartupMilliseconds;
        var secondResponse = engine.Recognize(
            Request(imagePath, new PhysicalSize(1000, 300)));

        Assert.Equal(OcrEngineIds.PaddleSharp, response.Engine);
        Assert.True(startupMilliseconds > 0);
        Assert.Equal(startupMilliseconds, engine.StartupMilliseconds);
        Assert.Contains(
            response.Blocks,
            block => block.Text.Contains(
                "ShotLens",
                StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(secondResponse.Blocks);
    }

    public void Dispose()
    {
        Directory.Delete(tempDirectory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string CreatePng(int width, int height)
    {
        var path = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}.png");
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
        return path;
    }

    private static OcrProtocolRequest Request(
        string imagePath,
        PhysicalSize imageSize) =>
        new(
            OcrProtocol.CurrentVersion,
            Guid.NewGuid().ToString("N"),
            OcrEngineIds.PaddleSharp,
            imagePath,
            imageSize,
            ["zh", "en"]);
}
