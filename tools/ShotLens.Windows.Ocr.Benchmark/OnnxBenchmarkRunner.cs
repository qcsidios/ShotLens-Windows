using System.IO;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Ocr.Worker.Benchmark;
using ShotLens.Windows.Ocr.Worker.Onnx;

namespace ShotLens.Windows.Ocr.Benchmark;

internal static class OnnxBenchmarkRunner
{
    public static void Run(
        string datasetDirectory,
        string modelBaseDirectory,
        string reportDirectory,
        string publishedDirectory)
    {
        using var engine = new OnnxPaddleOcrEngine(modelBaseDirectory);
        var modelDirectory = OnnxOcrModelCatalog.ResolveDirectory(
            modelBaseDirectory);
        BenchmarkRunner.Run(
            datasetDirectory,
            reportDirectory,
            OcrEngineIds.OnnxPaddleOcr,
            OnnxPaddleOcrEngine.Version,
            OnnxOcrModelCatalog.Version,
            engine.Recognize,
            () => engine.StartupMilliseconds,
            () => OcrCandidatePublishedSizeCalculator.Calculate(
                publishedDirectory,
                OcrEngineIds.OnnxPaddleOcr),
            () => OnnxOcrModelCatalog.Files.Sum(
                model => new FileInfo(
                    Path.Combine(modelDirectory, model.FileName)).Length),
            "ONNX");
    }
}
