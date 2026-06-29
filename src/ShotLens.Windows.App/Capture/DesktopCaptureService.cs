using System.Drawing;
using System.Windows.Forms;

namespace ShotLens.Windows.App.Capture;

public sealed class DesktopCaptureService
{
    public DesktopSnapshot Capture()
    {
        var bounds = VirtualBounds();
        var bitmap = new Bitmap(
            bounds.Width,
            bounds.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(
            bounds.Location,
            Point.Empty,
            bounds.Size,
            CopyPixelOperation.SourceCopy);
        return new DesktopSnapshot(bitmap, bounds);
    }

    private static Rectangle VirtualBounds()
    {
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            throw new InvalidOperationException("未找到可截图的显示器。");
        }

        var bounds = screens[0].Bounds;
        foreach (var screen in screens.Skip(1))
        {
            bounds = Rectangle.Union(bounds, screen.Bounds);
        }

        return bounds;
    }
}
