using System.Drawing;
using System.Windows.Forms;

namespace ShotLens.Windows.App.Services;

public static class ScreenCaptureService
{
    public static CapturedImage CaptureVirtualScreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
        return new CapturedImage(bitmap, bounds);
    }

    public static CapturedImage Crop(CapturedImage source, Rectangle selection)
    {
        var relative = new Rectangle(
            selection.X - source.ScreenBounds.X,
            selection.Y - source.ScreenBounds.Y,
            selection.Width,
            selection.Height);
        relative.Intersect(new Rectangle(0, 0, source.Bitmap.Width, source.Bitmap.Height));
        if (relative.Width <= 0 || relative.Height <= 0)
        {
            throw new InvalidOperationException("截图区域为空。");
        }

        var cropped = source.Bitmap.Clone(relative, source.Bitmap.PixelFormat);
        return new CapturedImage(cropped, selection);
    }
}
