using RapidOcrNet;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Worker.Onnx;

public static class OnnxOcrResultMapper
{
    public static OcrTextBlock[] Map(
        IReadOnlyList<TextBlock> blocks,
        SKBitmap source,
        double scale)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(source);
        var sourceSize = new PhysicalSize(source.Width, source.Height);
        return blocks.Select(
                (block, order) =>
                {
                    var bounds = OnnxOcrCoordinateMapper.RestoreBounds(
                        block.BoxPoints,
                        scale,
                        sourceSize);
                    return new OcrTextBlock(
                        block.Text,
                        bounds,
                        Confidence(block),
                        Language(block.Text),
                        bounds.Height,
                        Brightness(source, bounds),
                        order);
                })
            .ToArray();
    }

    private static double Confidence(TextBlock block)
    {
        var value = block.CharScores is { Length: > 0 }
            ? block.CharScores.Average(score => (double)score)
            : block.BoxScore;
        return Math.Clamp(value, 0, 1);
    }

    private static string Language(string text)
    {
        var hasChinese = text.Any(
            character =>
                character is >= '\u3400' and <= '\u9fff');
        var hasEnglish = text.Any(
            character =>
                character is (>= 'A' and <= 'Z')
                    or (>= 'a' and <= 'z'));
        return (hasChinese, hasEnglish) switch
        {
            (true, true) => "zh-en",
            (true, false) => "zh",
            (false, true) => "en",
            _ => "und"
        };
    }

    private static double Brightness(
        SKBitmap source,
        PhysicalRect bounds)
    {
        double sum = 0;
        for (var y = bounds.Y; y < bounds.Bottom; y++)
        {
            for (var x = bounds.X; x < bounds.Right; x++)
            {
                var color = source.GetPixel(x, y);
                sum += (2126 * color.Red
                    + 7152 * color.Green
                    + 722 * color.Blue)
                    / (10000d * byte.MaxValue);
            }
        }

        return sum / (bounds.Width * bounds.Height);
    }
}
