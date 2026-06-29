using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Ocr.Worker.Benchmark;

namespace ShotLens.Windows.Ocr.Tests.Benchmark;

public sealed class OcrCandidatePublishedSizeCalculatorTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"shotlens-candidate-size-{Guid.NewGuid():N}");

    public OcrCandidatePublishedSizeCalculatorTests()
    {
        Directory.CreateDirectory(directory);
        Write("common-shared.dll", 10);
        Write("RapidOcrNet.dll", 20);
        Write("Sdcb.PaddleOCR.dll", 30);
        Write("onnxruntime.dll", 40);
        Write("paddle_inference_c.dll", 50);
        Write(Path.Combine("models", "onnx-paddleocr", "model.onnx"), 60);
    }

    [Fact]
    public void Onnx_size_excludes_paddle_opencv_and_mkl_files()
    {
        var size = OcrCandidatePublishedSizeCalculator.Calculate(
            directory,
            OcrEngineIds.OnnxPaddleOcr);

        Assert.Equal(130, size);
    }

    [Fact]
    public void Paddle_size_excludes_onnx_runtime_and_models()
    {
        var size = OcrCandidatePublishedSizeCalculator.Calculate(
            directory,
            OcrEngineIds.PaddleSharp);

        Assert.Equal(90, size);
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private void Write(string relativePath, int size)
    {
        var path = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[size]);
    }
}
