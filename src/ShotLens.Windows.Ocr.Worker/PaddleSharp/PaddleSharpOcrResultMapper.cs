using OpenCvSharp;
using Sdcb.PaddleOCR;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Worker.PaddleSharp;

public static class PaddleSharpOcrResultMapper
{
    public static OcrTextBlock[] Map(
        IReadOnlyList<PaddleOcrResultRegion> regions,
        SKBitmap source)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(source);
        var sourceSize = new PhysicalSize(source.Width, source.Height);
        return regions.Select(
                (region, order) =>
                {
                    var bounds = RestoreBounds(region.Rect, sourceSize);
                    return new OcrTextBlock(
                        region.Text,
                        bounds,
                        Math.Clamp(region.Score, 0, 1),
                        Language(region.Text),
                        bounds.Height,
                        Brightness(source, bounds),
                        order);
                })
            .ToArray();
    }

    private static PhysicalRect RestoreBounds(
        RotatedRect rect,
        PhysicalSize sourceSize)
    {
        var points = rect.Points();
        var left = Math.Clamp(
            (int)Math.Floor(points.Min(point => point.X)),
            0,
            sourceSize.Width - 1);
        var top = Math.Clamp(
            (int)Math.Floor(points.Min(point => point.Y)),
            0,
            sourceSize.Height - 1);
        var right = Math.Clamp(
            (int)Math.Ceiling(points.Max(point => point.X)),
            left + 1,
            sourceSize.Width);
        var bottom = Math.Clamp(
            (int)Math.Ceiling(points.Max(point => point.Y)),
            top + 1,
            sourceSize.Height);
        return new PhysicalRect(
            left,
            top,
            right - left,
            bottom - top);
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
