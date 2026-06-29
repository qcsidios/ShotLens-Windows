using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Ocr.Worker.Onnx;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Tests.Onnx;

public sealed class OnnxPaddleOcrEngineTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"shotlens-onnx-engine-{Guid.NewGuid():N}");

    public OnnxPaddleOcrEngineTests() =>
        Directory.CreateDirectory(tempDirectory);

    [Fact]
    public void Request_size_must_match_decoded_source_pixels()
    {
        var imagePath = CreatePng(100, 50);
        var engine = new OnnxPaddleOcrEngine(tempDirectory);
        var request = Request(imagePath, new PhysicalSize(99, 50));

        var exception = Assert.Throws<OcrProtocolException>(
            () => engine.Recognize(request));

        Assert.Contains("尺寸", exception.Message);
    }

    [Fact]
    public void Missing_verified_models_fail_before_inference()
    {
        var imagePath = CreatePng(100, 50);
        var engine = new OnnxPaddleOcrEngine(tempDirectory);
        var request = Request(imagePath, new PhysicalSize(100, 50));

        Assert.Throws<FileNotFoundException>(
            () => engine.Recognize(request));
    }

    [Fact]
    public void Verified_models_run_real_onnx_inference()
    {
        if (Environment.GetEnvironmentVariable(
                "SHOTLENS_RUN_OCR_MODEL_TESTS") != "1")
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

        var repositoryRoot = FindRepositoryRoot();
        using var engine = new OnnxPaddleOcrEngine(
            Path.Combine(
                repositoryRoot,
                "src",
                "ShotLens.Windows.Ocr.Worker"));

        var response = engine.Recognize(
            Request(imagePath, new PhysicalSize(1000, 300)));
        var startupMilliseconds = engine.StartupMilliseconds;
        var secondResponse = engine.Recognize(
            Request(imagePath, new PhysicalSize(1000, 300)));

        Assert.Equal(OcrEngineIds.OnnxPaddleOcr, response.Engine);
        Assert.True(startupMilliseconds > 0);
        Assert.Equal(startupMilliseconds, engine.StartupMilliseconds);
        Assert.NotEmpty(secondResponse.Blocks);
        Assert.Contains(
            response.Blocks,
            block => block.Text.Contains(
                "ShotLens",
                StringComparison.OrdinalIgnoreCase));
        Assert.All(
            response.Blocks,
            block =>
            {
                Assert.InRange(block.Bounds.X, 0, 999);
                Assert.InRange(block.Bounds.Y, 0, 299);
                Assert.InRange(block.Bounds.Right, 1, 1000);
                Assert.InRange(block.Bounds.Bottom, 1, 300);
            });
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
            OcrEngineIds.OnnxPaddleOcr,
            imagePath,
            imageSize,
            ["zh", "en"]);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(
                    Path.Combine(directory.FullName, "ShotLens.Windows.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("未找到 ShotLens 仓库根目录。");
    }
}
