namespace ShotLens.Windows.Core.Ocr.Benchmark;

public static class OcrBenchmarkReportBuilder
{
    public static OcrBenchmarkReport Build(
        OcrBenchmarkDatasetManifest manifest,
        string engine,
        string engineVersion,
        string modelVersion,
        string datasetSha256,
        IReadOnlyList<OcrBenchmarkSampleRun> runs,
        DateTimeOffset generatedAtUtc,
        double startupMilliseconds,
        long publishedSizeBytes,
        long modelSizeBytes,
        OcrBenchmarkMachine machine)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(runs);
        var runById = runs
            .GroupBy(run => run.SampleId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.Ordinal);
        if (runs.Count != manifest.Samples.Length
            || runById.Count != manifest.Samples.Length
            || runById.Any(pair => pair.Value.Length != 1)
            || manifest.Samples.Any(
                sample => !runById.ContainsKey(sample.Id)))
        {
            throw new ArgumentException(
                "OCR 基准运行结果必须与样本一一对应。",
                nameof(runs));
        }

        var totalCharacters = 0;
        var totalEditDistance = 0;
        var totalLines = 0;
        var matchedLines = 0;
        double totalOrderAccuracy = 0;
        var missedParagraphs = new List<string>();
        foreach (var sample in manifest.Samples)
        {
            var run = runById[sample.Id][0];
            var expected = sample.ExpectedLines
                .OrderBy(line => line.Order)
                .Select(line => line.Text)
                .ToArray();
            var score = OcrBenchmarkScorer.Score(expected, run.Blocks);
            totalCharacters += score.ExpectedCharacterCount;
            totalEditDistance += score.EditDistance;
            totalLines += expected.Length;
            matchedLines += expected.Length - score.MissingLines.Length;
            totalOrderAccuracy += score.ReadingOrderAccuracy;
            if (run.Blocks.Length == 0)
            {
                missedParagraphs.Add(sample.Id);
            }
        }

        return new OcrBenchmarkReport(
            2,
            generatedAtUtc,
            engine,
            engineVersion,
            modelVersion,
            datasetSha256,
            manifest.Samples.Length,
            Math.Max(
                0,
                1 - (double)totalEditDistance / totalCharacters),
            (double)matchedLines / totalLines,
            totalOrderAccuracy / manifest.Samples.Length,
            LanguageMetrics(manifest, runById),
            startupMilliseconds,
            Median(runs.Select(run => run.OcrMilliseconds)),
            Percentile95(runs.Select(run => run.OcrMilliseconds)),
            Median(runs.Select(run => run.TotalMilliseconds)),
            runs.Max(run => run.PeakMemoryBytes),
            publishedSizeBytes,
            modelSizeBytes,
            machine,
            missedParagraphs.ToArray());
    }

    private static OcrBenchmarkLanguageMetric[] LanguageMetrics(
        OcrBenchmarkDatasetManifest manifest,
        IReadOnlyDictionary<string, OcrBenchmarkSampleRun[]> runById) =>
        manifest.Samples
            .GroupBy(sample => sample.Language)
            .OrderBy(group => group.Key)
            .Select(
                group =>
                {
                    var characters = 0;
                    var edits = 0;
                    var lines = 0;
                    var matched = 0;
                    double order = 0;
                    foreach (var sample in group)
                    {
                        var expected = sample.ExpectedLines
                            .OrderBy(line => line.Order)
                            .Select(line => line.Text)
                            .ToArray();
                        var score = OcrBenchmarkScorer.Score(
                            expected,
                            runById[sample.Id][0].Blocks);
                        characters += score.ExpectedCharacterCount;
                        edits += score.EditDistance;
                        lines += expected.Length;
                        matched += expected.Length
                            - score.MissingLines.Length;
                        order += score.ReadingOrderAccuracy;
                    }

                    return new OcrBenchmarkLanguageMetric(
                        group.Key,
                        group.Count(),
                        Math.Max(0, 1 - (double)edits / characters),
                        (double)matched / lines,
                        order / group.Count());
                })
            .ToArray();

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2
            : ordered[middle];
    }

    private static double Percentile95(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        var index = Math.Max(0, (int)Math.Ceiling(ordered.Length * 0.95) - 1);
        return ordered[index];
    }
}

public sealed record OcrBenchmarkSampleRun(
    string SampleId,
    OcrTextBlock[] Blocks,
    double OcrMilliseconds,
    double TotalMilliseconds,
    long PeakMemoryBytes);
