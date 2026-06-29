using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ShotLens.Windows.Core;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace ShotLens.Windows.App.Services;

public static class WindowsOcrService
{
    public static async Task<IReadOnlyList<OcrTextBlock>> RecognizeAsync(Bitmap bitmap)
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? OcrEngine.TryCreateFromLanguage(new Language("en"));
        if (engine is null)
        {
            throw new InvalidOperationException("当前 Windows 未安装可用 OCR 语言。请在 Windows 设置中安装文本识别语言包。");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"ShotLens-{Guid.NewGuid():N}.png");
        try
        {
            bitmap.Save(tempPath, ImageFormat.Png);
            var file = await StorageFile.GetFileFromPathAsync(tempPath);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var result = await engine.RecognizeAsync(softwareBitmap);

            return result.Lines
                .Select(line => ToTextBlock(line))
                .Where(block => !string.IsNullOrWhiteSpace(block.Text))
                .ToArray();
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
                // 临时文件删除失败不影响截图翻译。
            }
        }
    }

    private static OcrTextBlock ToTextBlock(OcrLine line)
    {
        var rects = line.Words.Select(word => word.BoundingRect).ToArray();
        if (rects.Length == 0)
        {
            return new OcrTextBlock(line.Text, 0, 0, 1, 1, "und");
        }

        var left = rects.Min(rect => rect.X);
        var top = rects.Min(rect => rect.Y);
        var right = rects.Max(rect => rect.X + rect.Width);
        var bottom = rects.Max(rect => rect.Y + rect.Height);
        return new OcrTextBlock(
            NormalizeOcrText(line.Text),
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top),
            DetectLanguage(line.Text));
    }

    private static string NormalizeOcrText(string text) =>
        text.Replace("Al", "AI", StringComparison.Ordinal);

    private static string DetectLanguage(string text)
    {
        if (text.Any(ch => ch >= '\u4e00' && ch <= '\u9fff'))
        {
            return "zh-Hans";
        }

        if (text.Any(ch => ch >= '\u3040' && ch <= '\u30ff'))
        {
            return "ja";
        }

        if (text.Any(ch => ch >= '\uac00' && ch <= '\ud7af'))
        {
            return "ko";
        }

        return text.Any(char.IsLetter) ? "en" : "und";
    }
}
