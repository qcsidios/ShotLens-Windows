using System.Security.Cryptography;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr.Benchmark;

namespace ShotLens.Windows.Ocr.Tests.Benchmark;

public sealed class OcrBenchmarkDatasetValidatorTests : IDisposable
{
    private readonly string _datasetDirectory =
        Path.Combine(Path.GetTempPath(), $"shotlens-dataset-{Guid.NewGuid():N}");
    private readonly string _imageHash;

    public OcrBenchmarkDatasetValidatorTests()
    {
        Directory.CreateDirectory(_datasetDirectory);
        var imagePath = Path.Combine(_datasetDirectory, "sample.png");
        File.WriteAllBytes(
            imagePath,
            [
                137, 80, 78, 71, 13, 10, 26, 10,
                0, 0, 0, 13,
                73, 72, 68, 82,
                0, 0, 5, 0,
                0, 0, 2, 208
            ]);
        _imageHash = Convert.ToHexString(
            SHA256.HashData(File.ReadAllBytes(imagePath))).ToLowerInvariant();
    }

    [Fact]
    public void Accepts_thirty_english_twenty_chinese_and_ten_mixed_samples()
    {
        var manifest = ValidManifest();

        OcrBenchmarkDatasetValidator.Validate(
            manifest,
            _datasetDirectory);
    }

    [Fact]
    public void Rejects_missing_image_empty_annotation_duplicate_id_and_sensitive_data()
    {
        var manifest = ValidManifest();
        var invalidSamples = new[]
        {
            manifest.Samples[0] with { ImageFile = "missing.png" },
            manifest.Samples[0] with { ExpectedLines = [] },
            manifest.Samples[0] with { Id = manifest.Samples[1].Id },
            manifest.Samples[0] with { ContainsSensitiveData = true }
        };

        Assert.All(
            invalidSamples,
            invalid => Assert.Throws<OcrBenchmarkValidationException>(
                () => OcrBenchmarkDatasetValidator.Validate(
                    manifest with
                    {
                        Samples = ReplaceFirst(manifest.Samples, invalid)
                    },
                    _datasetDirectory)));
    }

    [Fact]
    public void Rejects_language_content_that_does_not_match_label()
    {
        var manifest = ValidManifest();
        var invalid = manifest.Samples[0] with
        {
            ExpectedLines = [new OcrBenchmarkExpectedLine("中文", 0)]
        };

        Assert.Throws<OcrBenchmarkValidationException>(
            () => OcrBenchmarkDatasetValidator.Validate(
                manifest with
                {
                    Samples = ReplaceFirst(manifest.Samples, invalid)
                },
                _datasetDirectory));
    }

    [Fact]
    public void Rejects_candidate_hash_that_differs_from_manifest()
    {
        var manifest = ValidManifest();
        var hashes = manifest.Samples.ToDictionary(
            sample => sample.Id,
            sample => sample.ImageSha256);
        hashes[manifest.Samples[0].Id] = new string('0', 64);

        Assert.Throws<OcrBenchmarkValidationException>(
            () => OcrBenchmarkDatasetValidator.ValidateCandidateHashes(
                manifest,
                hashes));
    }

    public void Dispose() =>
        Directory.Delete(_datasetDirectory, recursive: true);

    private OcrBenchmarkDatasetManifest ValidManifest()
    {
        var samples = new List<OcrBenchmarkSample>();
        AddSamples(samples, "en", 30, OcrBenchmarkLanguage.English, "Settings");
        AddSamples(samples, "zh", 20, OcrBenchmarkLanguage.Chinese, "设置");
        AddSamples(samples, "mixed", 10, OcrBenchmarkLanguage.Mixed, "设置 Settings");
        return new OcrBenchmarkDatasetManifest(
            "1",
            20260629,
            "WPF",
            new OcrBenchmarkRenderSettings(
                "Segoe UI",
                "Microsoft YaHei UI",
                new PhysicalSize(1280, 720),
                [96, 120, 144, 192]),
            samples.ToArray());
    }

    private void AddSamples(
        List<OcrBenchmarkSample> samples,
        string prefix,
        int count,
        OcrBenchmarkLanguage language,
        string text)
    {
        for (var index = 1; index <= count; index++)
        {
            samples.Add(
                new OcrBenchmarkSample(
                    $"{prefix}-{index:000}",
                    "sample.png",
                    language,
                    index % 2 == 0
                        ? OcrBenchmarkTheme.Light
                        : OcrBenchmarkTheme.Dark,
                    OcrBenchmarkLayout.DenseParagraph,
                    96,
                    new PhysicalSize(1280, 720),
                    12,
                    [new OcrBenchmarkExpectedLine(text, 0)],
                    _imageHash,
                    false));
        }
    }

    private static OcrBenchmarkSample[] ReplaceFirst(
        OcrBenchmarkSample[] samples,
        OcrBenchmarkSample replacement)
    {
        var result = samples.ToArray();
        result[0] = replacement;
        return result;
    }
}
