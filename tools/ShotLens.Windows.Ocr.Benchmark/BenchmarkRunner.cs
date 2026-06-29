using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Core.Ocr.Benchmark;

namespace ShotLens.Windows.Ocr.Benchmark;

internal static class BenchmarkRunner
{
    public static void Run(
        string datasetDirectory,
        string reportDirectory,
        string engineId,
        string engineVersion,
        string modelVersion,
        Func<OcrProtocolRequest, OcrProtocolResponse> recognize,
        Func<double> startupMilliseconds,
        Func<long> publishedSizeBytes,
        Func<long> modelSizeBytes,
        string displayName)
    {
        var manifestPath = Path.Combine(datasetDirectory, "manifest.json");
        var manifest = OcrBenchmarkManifestJson.Deserialize(
            File.ReadAllText(manifestPath));
        OcrBenchmarkDatasetValidator.Validate(manifest, datasetDirectory);
        var runs = new List<OcrBenchmarkSampleRun>();
        var details = new List<SampleDetail>();
        var candidateHashes = new Dictionary<string, string>(
            StringComparer.Ordinal);
        foreach (var sample in manifest.Samples)
        {
            var imagePath = Path.Combine(datasetDirectory, sample.ImageFile);
            candidateHashes.Add(sample.Id, FileSha256(imagePath));
            var total = Stopwatch.StartNew();
            var response = recognize(
                new OcrProtocolRequest(
                    OcrProtocol.CurrentVersion,
                    Guid.NewGuid().ToString("N"),
                    engineId,
                    imagePath,
                    sample.ViewportPixels,
                    LanguageHints(sample.Language)));
            total.Stop();
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            runs.Add(
                new OcrBenchmarkSampleRun(
                    sample.Id,
                    response.Blocks,
                    response.ElapsedMilliseconds,
                    total.Elapsed.TotalMilliseconds,
                    Math.Max(
                        process.PeakWorkingSet64,
                        process.WorkingSet64)));
            details.Add(
                new SampleDetail(
                    sample.Id,
                    sample.ExpectedLines
                        .OrderBy(line => line.Order)
                        .Select(line => line.Text)
                        .ToArray(),
                    response.Blocks
                        .OrderBy(block => block.Order)
                        .Select(block => block.Text)
                        .ToArray(),
                    response.ElapsedMilliseconds,
                    total.Elapsed.TotalMilliseconds));
            Console.WriteLine(
                $"[{runs.Count:00}/{manifest.Samples.Length}] {sample.Id}：{response.Blocks.Length} 块，{total.Elapsed.TotalMilliseconds:F0} ms");
        }

        OcrBenchmarkDatasetValidator.ValidateCandidateHashes(
            manifest,
            candidateHashes);
        var report = OcrBenchmarkReportBuilder.Build(
            manifest,
            engineId,
            engineVersion,
            modelVersion,
            DatasetSha256(manifest),
            runs,
            DateTimeOffset.UtcNow,
            startupMilliseconds(),
            publishedSizeBytes(),
            modelSizeBytes(),
            Machine(manifest));
        OcrBenchmarkReportWriter.Write(reportDirectory, report);
        File.WriteAllText(
            Path.Combine(reportDirectory, "样本结果.json"),
            JsonSerializer.Serialize(
                details,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));
        Console.WriteLine(
            $"{displayName} OCR 基准报告已生成：{reportDirectory}");
    }

    private static string[] LanguageHints(OcrBenchmarkLanguage language) =>
        language switch
        {
            OcrBenchmarkLanguage.English => ["en"],
            OcrBenchmarkLanguage.Chinese => ["zh"],
            OcrBenchmarkLanguage.Mixed => ["zh", "en"],
            _ => []
        };

    private static string FileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(
            SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string DatasetSha256(
        OcrBenchmarkDatasetManifest manifest)
    {
        var value = string.Join(
            '\n',
            manifest.Samples
                .OrderBy(sample => sample.Id, StringComparer.Ordinal)
                .Select(
                    sample =>
                        $"{sample.Id}:{sample.ImageSha256.ToLowerInvariant()}"));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();
    }

    private static OcrBenchmarkMachine Machine(
        OcrBenchmarkDatasetManifest manifest) =>
        new(
            RuntimeInformation.OSDescription,
            Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
                ?? RuntimeInformation.ProcessArchitecture.ToString(),
            GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            Environment.GetEnvironmentVariable("SHOTLENS_BENCHMARK_GPU")
                ?? "未采集",
            Environment.GetEnvironmentVariable(
                    "SHOTLENS_BENCHMARK_POWER_MODE")
                ?? "未采集",
            string.Join(
                ", ",
                manifest.RenderSettings.DpiValues.Select(
                    dpi => $"{dpi / 96d:P0}")));

    private sealed record SampleDetail(
        string SampleId,
        string[] ExpectedLines,
        string[] ActualBlocks,
        double OcrMilliseconds,
        double TotalMilliseconds);
}
