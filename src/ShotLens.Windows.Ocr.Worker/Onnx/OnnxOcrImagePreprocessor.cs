using ShotLens.Windows.Core.Capture;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Worker.Onnx;

public static class OnnxOcrImagePreprocessor
{
    public static PreparedOnnxOcrImage Prepare(
        SKBitmap source,
        int minimumShortSide,
        int maximumLongSide)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (minimumShortSide <= 0 || maximumLongSide < minimumShortSide)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumShortSide),
                "OCR 缩放边界无效。");
        }

        var shortSide = Math.Min(source.Width, source.Height);
        var longSide = Math.Max(source.Width, source.Height);
        var scale = longSide > maximumLongSide
            ? (double)maximumLongSide / longSide
            : shortSide < minimumShortSide
                ? Math.Min(
                    (double)minimumShortSide / shortSide,
                    (double)maximumLongSide / longSide)
                : 1;
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var bitmap = new SKBitmap(
            new SKImageInfo(
                width,
                height,
                SKColorType.Bgra8888,
                SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.DrawBitmap(source, new SKRect(0, 0, width, height));
        return new PreparedOnnxOcrImage(bitmap, scale);
    }
}

public sealed class PreparedOnnxOcrImage(
    SKBitmap bitmap,
    double scale) : IDisposable
{
    public SKBitmap Bitmap { get; } = bitmap;

    public double Scale { get; } = scale;

    public void Dispose() => Bitmap.Dispose();
}

public static class OnnxOcrCoordinateMapper
{
    public static PhysicalRect RestoreBounds(
        IReadOnlyCollection<SKPointI> points,
        double scale,
        PhysicalSize sourceSize)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0
            || !double.IsFinite(scale)
            || scale <= 0
            || sourceSize.Width <= 0
            || sourceSize.Height <= 0)
        {
            throw new ArgumentException("OCR 坐标还原参数无效。");
        }

        var left = Math.Clamp(
            (int)Math.Floor(points.Min(point => point.X) / scale),
            0,
            sourceSize.Width - 1);
        var top = Math.Clamp(
            (int)Math.Floor(points.Min(point => point.Y) / scale),
            0,
            sourceSize.Height - 1);
        var right = Math.Clamp(
            (int)Math.Ceiling(points.Max(point => point.X) / scale),
            left + 1,
            sourceSize.Width);
        var bottom = Math.Clamp(
            (int)Math.Ceiling(points.Max(point => point.Y) / scale),
            top + 1,
            sourceSize.Height);
        return new PhysicalRect(
            left,
            top,
            right - left,
            bottom - top);
    }
}
