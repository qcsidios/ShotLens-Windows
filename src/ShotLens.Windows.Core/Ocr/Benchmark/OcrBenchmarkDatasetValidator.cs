using System.Security.Cryptography;
using ShotLens.Windows.Core.Imaging;

namespace ShotLens.Windows.Core.Ocr.Benchmark;

public static class OcrBenchmarkDatasetValidator
{
    public static void Validate(
        OcrBenchmarkDatasetManifest manifest,
        string datasetDirectory)
    {
        if (string.IsNullOrWhiteSpace(manifest.GeneratorVersion)
            || string.IsNullOrWhiteSpace(manifest.Renderer)
            || manifest.RenderSettings is null
            || manifest.Samples is null)
        {
            throw new OcrBenchmarkValidationException("基准集元数据不完整。");
        }

        if (string.IsNullOrWhiteSpace(manifest.RenderSettings.EnglishFont)
            || string.IsNullOrWhiteSpace(manifest.RenderSettings.ChineseFont)
            || manifest.RenderSettings.LogicalViewport.Width <= 0
            || manifest.RenderSettings.LogicalViewport.Height <= 0
            || manifest.RenderSettings.DpiValues is null
            || manifest.RenderSettings.DpiValues.Length == 0
            || manifest.RenderSettings.DpiValues.Any(dpi => dpi <= 0))
        {
            throw new OcrBenchmarkValidationException("基准集渲染参数无效。");
        }

        ValidateRequiredCounts(manifest.Samples);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sample in manifest.Samples)
        {
            if (!ids.Add(sample.Id))
            {
                throw new OcrBenchmarkValidationException(
                    $"基准样本 ID 重复：{sample.Id}。");
            }

            ValidateSample(sample, datasetDirectory);
        }
    }

    public static void ValidateCandidateHashes(
        OcrBenchmarkDatasetManifest manifest,
        IReadOnlyDictionary<string, string> candidateHashes)
    {
        if (candidateHashes.Count != manifest.Samples.Length)
        {
            throw new OcrBenchmarkValidationException(
                "候选引擎读取的样本数量不一致。");
        }

        foreach (var sample in manifest.Samples)
        {
            if (!candidateHashes.TryGetValue(sample.Id, out var hash)
                || !string.Equals(
                    hash,
                    sample.ImageSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new OcrBenchmarkValidationException(
                    $"候选引擎图片校验和不一致：{sample.Id}。");
            }
        }
    }

    private static void ValidateRequiredCounts(OcrBenchmarkSample[] samples)
    {
        if (samples.Length != 60
            || samples.Count(sample =>
                sample.Language == OcrBenchmarkLanguage.English) != 30
            || samples.Count(sample =>
                sample.Language == OcrBenchmarkLanguage.Chinese) != 20
            || samples.Count(sample =>
                sample.Language == OcrBenchmarkLanguage.Mixed) != 10)
        {
            throw new OcrBenchmarkValidationException(
                "基准集必须包含 30 英文、20 中文和 10 混合样本。");
        }
    }

    private static void ValidateSample(
        OcrBenchmarkSample sample,
        string datasetDirectory)
    {
        if (string.IsNullOrWhiteSpace(sample.Id)
            || sample.ContainsSensitiveData
            || sample.Dpi <= 0
            || sample.ViewportPixels.Width <= 0
            || sample.ViewportPixels.Height <= 0
            || !double.IsFinite(sample.MinimumTextHeightPixels)
            || sample.MinimumTextHeightPixels <= 0)
        {
            throw new OcrBenchmarkValidationException(
                $"基准样本元数据无效：{sample.Id}。");
        }

        if (sample.ExpectedLines is null || sample.ExpectedLines.Length == 0)
        {
            throw new OcrBenchmarkValidationException(
                $"基准样本标注为空：{sample.Id}。");
        }

        var orders = new HashSet<int>();
        foreach (var line in sample.ExpectedLines)
        {
            if (string.IsNullOrWhiteSpace(line.Text)
                || line.Order < 0
                || line.Order >= sample.ExpectedLines.Length
                || !orders.Add(line.Order))
            {
                throw new OcrBenchmarkValidationException(
                    $"基准样本顺序标注无效：{sample.Id}。");
            }
        }

        ValidateLanguage(sample);
        var imagePath = ResolveImagePath(datasetDirectory, sample.ImageFile);
        if (!File.Exists(imagePath))
        {
            throw new OcrBenchmarkValidationException(
                $"基准图片不存在：{sample.ImageFile}。");
        }

        if (PngDimensions.Read(imagePath) != sample.ViewportPixels)
        {
            throw new OcrBenchmarkValidationException(
                $"基准图片尺寸错误：{sample.Id}。");
        }

        using var stream = File.OpenRead(imagePath);
        var actualHash = Convert.ToHexString(
            SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(
                actualHash,
                sample.ImageSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new OcrBenchmarkValidationException(
                $"基准图片校验和错误：{sample.Id}。");
        }
    }

    private static void ValidateLanguage(OcrBenchmarkSample sample)
    {
        var text = string.Concat(
            sample.ExpectedLines
                .OrderBy(line => line.Order)
                .Select(line => line.Text));
        var containsLatin = text.Any(character =>
            character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
        var containsChinese = text.Any(character =>
            character is >= '\u3400' and <= '\u9fff');
        var valid = sample.Language switch
        {
            OcrBenchmarkLanguage.English => containsLatin && !containsChinese,
            OcrBenchmarkLanguage.Chinese => containsChinese && !containsLatin,
            OcrBenchmarkLanguage.Mixed => containsLatin && containsChinese,
            _ => false
        };
        if (!valid)
        {
            throw new OcrBenchmarkValidationException(
                $"基准样本语言标签错误：{sample.Id}。");
        }
    }

    private static string ResolveImagePath(
        string datasetDirectory,
        string imageFile)
    {
        if (string.IsNullOrWhiteSpace(imageFile)
            || Path.IsPathFullyQualified(imageFile))
        {
            throw new OcrBenchmarkValidationException("基准图片路径无效。");
        }

        var root = Path.GetFullPath(datasetDirectory);
        var path = Path.GetFullPath(Path.Combine(root, imageFile));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : $"{root}{Path.DirectorySeparatorChar}";
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!path.StartsWith(rootPrefix, comparison))
        {
            throw new OcrBenchmarkValidationException(
                "基准图片不能位于数据集目录之外。");
        }

        return path;
    }
}
