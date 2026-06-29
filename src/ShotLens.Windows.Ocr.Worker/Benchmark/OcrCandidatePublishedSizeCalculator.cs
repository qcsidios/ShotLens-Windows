using ShotLens.Windows.Core.Ocr;

namespace ShotLens.Windows.Ocr.Worker.Benchmark;

public static class OcrCandidatePublishedSizeCalculator
{
    private static readonly HashSet<string> PaddleOnlyFiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "common.dll",
            "libiomp5md.dll",
            "mkldnn.dll",
            "mklml.dll",
            "opencv_videoio_ffmpeg4110_64.dll",
            "paddle2onnx.dll",
            "paddle_inference_c.dll",
            "phi.dll",
            "YamlDotNet.dll"
        };

    private static readonly HashSet<string> OnnxOnlyFiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Clipper2Lib.dll",
            "Microsoft.ML.OnnxRuntime.dll",
            "RapidOcrNet.dll",
            "System.Numerics.Tensors.dll"
        };

    public static long Calculate(
        string publishedDirectory,
        string engineId)
    {
        if (!Directory.Exists(publishedDirectory))
        {
            return 0;
        }

        if (engineId is not (
                OcrEngineIds.OnnxPaddleOcr
                or OcrEngineIds.PaddleSharp))
        {
            throw new ArgumentException(
                $"不支持计算发布体积的 OCR 引擎：{engineId}。",
                nameof(engineId));
        }

        return Directory.EnumerateFiles(
                publishedDirectory,
                "*",
                SearchOption.AllDirectories)
            .Where(
                path => engineId == OcrEngineIds.OnnxPaddleOcr
                    ? !IsPaddleOnly(path)
                    : !IsOnnxOnly(path, publishedDirectory))
            .Sum(path => new FileInfo(path).Length);
    }

    private static bool IsPaddleOnly(string path)
    {
        var fileName = Path.GetFileName(path);
        return PaddleOnlyFiles.Contains(fileName)
            || fileName.StartsWith(
                "Sdcb.Paddle",
                StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith(
                "OpenCvSharp",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOnnxOnly(
        string path,
        string publishedDirectory)
    {
        var fileName = Path.GetFileName(path);
        var relativePath = Path.GetRelativePath(
                publishedDirectory,
                path)
            .Replace(Path.DirectorySeparatorChar, '/');
        return OnnxOnlyFiles.Contains(fileName)
            || fileName.StartsWith(
                "onnxruntime",
                StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith(
                "models/onnx-paddleocr/",
                StringComparison.OrdinalIgnoreCase);
    }
}
