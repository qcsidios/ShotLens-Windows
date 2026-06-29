using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr.Benchmark;

namespace ShotLens.Windows.Ocr.Benchmark;

internal static class PlatformDatasetGenerator
{
    private const int RandomSeed = 20260629;
    private const string EnglishFont = "Segoe UI";
    private const string ChineseFont = "Microsoft YaHei UI";
    private static readonly PhysicalSize LogicalViewport = new(960, 540);
    private static readonly int[] DpiValues = [96, 120, 144, 192];
    private static readonly OcrBenchmarkLayout[] Layouts =
    [
        OcrBenchmarkLayout.SmallText,
        OcrBenchmarkLayout.Title,
        OcrBenchmarkLayout.Menu,
        OcrBenchmarkLayout.DenseParagraph,
        OcrBenchmarkLayout.Mixed
    ];

    public static void Generate(string outputDirectory)
    {
        if (Directory.Exists(outputDirectory)
            && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
        {
            throw new IOException("输出目录必须为空，避免覆盖已有文件。");
        }

        Directory.CreateDirectory(outputDirectory);
        var random = new Random(RandomSeed);
        var samples = new List<OcrBenchmarkSample>();
        AddLanguageSamples(
            samples,
            outputDirectory,
            random,
            "en",
            30,
            OcrBenchmarkLanguage.English,
            EnglishCorpus);
        AddLanguageSamples(
            samples,
            outputDirectory,
            random,
            "zh",
            20,
            OcrBenchmarkLanguage.Chinese,
            ChineseCorpus);
        AddLanguageSamples(
            samples,
            outputDirectory,
            random,
            "mixed",
            10,
            OcrBenchmarkLanguage.Mixed,
            MixedCorpus);

        var manifest = new OcrBenchmarkDatasetManifest(
            "1",
            RandomSeed,
            "WPF RenderTargetBitmap",
            new OcrBenchmarkRenderSettings(
                EnglishFont,
                ChineseFont,
                LogicalViewport,
                DpiValues),
            samples.ToArray());
        File.WriteAllText(
            Path.Combine(outputDirectory, "manifest.json"),
            OcrBenchmarkManifestJson.Serialize(manifest));
        File.WriteAllText(
            Path.Combine(outputDirectory, "数据集说明.md"),
            DatasetReadme());
        OcrBenchmarkDatasetValidator.Validate(manifest, outputDirectory);
    }

    private static void AddLanguageSamples(
        List<OcrBenchmarkSample> samples,
        string outputDirectory,
        Random random,
        string prefix,
        int count,
        OcrBenchmarkLanguage language,
        string[][] corpus)
    {
        for (var index = 1; index <= count; index++)
        {
            var id = $"{prefix}-{index:000}";
            var lines = corpus[(index - 1) % corpus.Length];
            var layout = Layouts[(index - 1) % Layouts.Length];
            var theme = index % 2 == 0
                ? OcrBenchmarkTheme.Light
                : OcrBenchmarkTheme.Dark;
            var dpi = DpiValues[(index - 1) % DpiValues.Length];
            var imageFile = $"{id}.png";
            var imagePath = Path.Combine(outputDirectory, imageFile);
            var minimumFontSize = Render(
                imagePath,
                lines,
                language,
                layout,
                theme,
                dpi,
                random);
            using var image = File.OpenRead(imagePath);
            var hash = Convert.ToHexString(
                SHA256.HashData(image)).ToLowerInvariant();
            var scale = dpi / 96d;
            samples.Add(
                new OcrBenchmarkSample(
                    id,
                    imageFile,
                    language,
                    theme,
                    layout,
                    dpi,
                    new PhysicalSize(
                        (int)Math.Round(LogicalViewport.Width * scale),
                        (int)Math.Round(LogicalViewport.Height * scale)),
                    minimumFontSize * scale,
                    lines.Select(
                        (text, order) =>
                            new OcrBenchmarkExpectedLine(text, order))
                        .ToArray(),
                    hash,
                    false));
        }
    }

    private static double Render(
        string imagePath,
        string[] lines,
        OcrBenchmarkLanguage language,
        OcrBenchmarkLayout layout,
        OcrBenchmarkTheme theme,
        int dpi,
        Random random)
    {
        var scale = dpi / 96d;
        var pixelWidth = (int)Math.Round(LogicalViewport.Width * scale);
        var pixelHeight = (int)Math.Round(LogicalViewport.Height * scale);
        var visual = new DrawingVisual();
        var minimumFontSize = double.MaxValue;
        using (var drawing = visual.RenderOpen())
        {
            var dark = theme == OcrBenchmarkTheme.Dark;
            var background = dark
                ? Color.FromRgb(24, 25, 28)
                : Color.FromRgb(242, 244, 247);
            var card = dark
                ? Color.FromRgb(40, 42, 47)
                : Colors.White;
            var foreground = dark
                ? Color.FromRgb(238, 239, 242)
                : Color.FromRgb(28, 31, 36);
            drawing.DrawRectangle(
                new SolidColorBrush(background),
                null,
                new Rect(0, 0, LogicalViewport.Width, LogicalViewport.Height));
            drawing.DrawRoundedRectangle(
                new SolidColorBrush(card),
                null,
                new Rect(48, 52, 864, 436),
                16,
                16);
            drawing.DrawEllipse(
                new SolidColorBrush(Color.FromRgb(68, 122, 255)),
                null,
                new Point(78, 80),
                9,
                9);

            var y = layout == OcrBenchmarkLayout.Title ? 108d : 116d;
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var fontSize = FontSize(layout, lineIndex);
                minimumFontSize = Math.Min(minimumFontSize, fontSize);
                var font = language == OcrBenchmarkLanguage.English
                    ? EnglishFont
                    : ChineseFont;
                var text = new FormattedText(
                    lines[lineIndex],
                    language == OcrBenchmarkLanguage.English
                        ? CultureInfo.GetCultureInfo("en-US")
                        : CultureInfo.GetCultureInfo("zh-CN"),
                    FlowDirection.LeftToRight,
                    new Typeface(
                        new FontFamily(font),
                        FontStyles.Normal,
                        lineIndex == 0
                            && layout == OcrBenchmarkLayout.Title
                            ? FontWeights.SemiBold
                            : FontWeights.Normal,
                        FontStretches.Normal),
                    fontSize,
                    new SolidColorBrush(foreground),
                    scale);
                var x = 76 + random.Next(0, 18);
                if (layout == OcrBenchmarkLayout.Menu)
                {
                    drawing.DrawRoundedRectangle(
                        new SolidColorBrush(
                            dark
                                ? Color.FromRgb(51, 54, 60)
                                : Color.FromRgb(247, 248, 250)),
                        null,
                        new Rect(68, y - 8, 810, text.Height + 16),
                        8,
                        8);
                }

                drawing.DrawText(text, new Point(x, y));
                y += text.Height + LineGap(layout);
            }
        }

        var bitmap = new RenderTargetBitmap(
            pixelWidth,
            pixelHeight,
            dpi,
            dpi,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(imagePath);
        encoder.Save(stream);
        return minimumFontSize;
    }

    private static double FontSize(
        OcrBenchmarkLayout layout,
        int lineIndex) =>
        layout switch
        {
            OcrBenchmarkLayout.SmallText => 11,
            OcrBenchmarkLayout.Title when lineIndex == 0 => 30,
            OcrBenchmarkLayout.Title => 16,
            OcrBenchmarkLayout.Menu => 16,
            OcrBenchmarkLayout.DenseParagraph => 13,
            OcrBenchmarkLayout.Mixed when lineIndex == 0 => 24,
            _ => 15
        };

    private static double LineGap(OcrBenchmarkLayout layout) =>
        layout switch
        {
            OcrBenchmarkLayout.DenseParagraph => 10,
            OcrBenchmarkLayout.Menu => 18,
            _ => 22
        };

    private static string DatasetReadme() =>
        """
        # ShotLens OCR 生成基准

        本目录由项目在 Windows 上使用 WPF、固定字体、固定视口、固定 DPI
        和固定随机种子生成。

        该基准只用于公平比较 OCR 技术路线，不代表完整真实用户场景。
        图片全部为合成内容，不包含用户数据或个人敏感信息。
        """;

    private static readonly string[][] EnglishCorpus =
    [
        ["ShotLens Settings", "Capture shortcut", "Translate selected area"],
        ["Quick actions", "Copy result", "Open history", "Close window"],
        ["Update available", "Download and restart", "Remind me later"],
        ["Reading mode", "Keep original layout", "Show translation"],
        ["Connection test", "API service is ready", "Save changes"],
        ["Desktop capture", "Choose a screen", "Drag to select text"]
    ];

    private static readonly string[][] ChineseCorpus =
    [
        ["截图翻译设置", "截图快捷键", "翻译选中区域"],
        ["快捷操作", "复制结果", "打开历史记录", "关闭窗口"],
        ["发现新版本", "下载并重新启动", "稍后提醒"],
        ["阅读模式", "保留原始布局", "显示翻译结果"],
        ["连接测试", "接口服务可用", "保存修改"]
    ];

    private static readonly string[][] MixedCorpus =
    [
        ["ShotLens 截图翻译", "Capture 快捷键", "Translate 选中区域"],
        ["API 连接测试", "Model 模型", "Save 保存设置"],
        ["Update 新版本", "Download 下载", "Restart 重新启动"],
        ["OCR 识别结果", "Copy 复制文字", "History 历史记录"],
        ["English 与中文", "Keep Layout 保留布局", "Close 关闭窗口"]
    ];
}
