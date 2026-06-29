namespace ShotLens.Windows.Core.Ocr.Benchmark;

public sealed record OcrBenchmarkReport(
    int SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    string Engine,
    string EngineVersion,
    string ModelVersion,
    string DatasetSha256,
    int SampleCount,
    double CharacterAccuracy,
    double LineRecall,
    double ReadingOrderAccuracy,
    OcrBenchmarkLanguageMetric[] LanguageMetrics,
    double StartupMilliseconds,
    double MedianOcrMilliseconds,
    double P95OcrMilliseconds,
    double MedianTotalMilliseconds,
    long PeakMemoryBytes,
    long PublishedSizeBytes,
    long ModelSizeBytes,
    OcrBenchmarkMachine Machine,
    string[] MissedParagraphSampleIds);

public sealed record OcrBenchmarkLanguageMetric(
    OcrBenchmarkLanguage Language,
    int SampleCount,
    double CharacterAccuracy,
    double LineRecall,
    double ReadingOrderAccuracy);

public sealed record OcrBenchmarkMachine(
    string WindowsVersion,
    string Cpu,
    long MemoryBytes,
    string Gpu,
    string PowerMode,
    string DpiConfiguration);
