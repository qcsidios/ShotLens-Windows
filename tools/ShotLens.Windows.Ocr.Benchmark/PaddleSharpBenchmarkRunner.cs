using System.IO;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Ocr.Worker.Benchmark;
using ShotLens.Windows.Ocr.Worker.PaddleSharp;

namespace ShotLens.Windows.Ocr.Benchmark;

internal static class PaddleSharpBenchmarkRunner
{
    public static void Run(
        string datasetDirectory,
        string reportDirectory,
        string publishedDirectory)
    {
        using var engine = new PaddleSharpOcrEngine();
        BenchmarkRunner.Run(
            datasetDirectory,
            reportDirectory,
            OcrEngineIds.PaddleSharp,
            PaddleSharpOcrEngine.Version,
            PaddleSharpOcrEngine.ModelVersion,
            engine.Recognize,
            () => engine.StartupMilliseconds,
            () => OcrCandidatePublishedSizeCalculator.Calculate(
                publishedDirectory,
                OcrEngineIds.PaddleSharp),
            () => Directory.Exists(publishedDirectory)
                ? Directory.EnumerateFiles(
                        publishedDirectory,
                        "Sdcb.PaddleOCR.Models.Local*.dll",
                        SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length)
                : 0,
            "PaddleSharp");
    }
}
