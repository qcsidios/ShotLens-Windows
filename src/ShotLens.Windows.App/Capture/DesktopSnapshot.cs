using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.App.Capture;

public sealed class DesktopSnapshot(
    System.Drawing.Bitmap bitmap,
    System.Drawing.Rectangle virtualBounds)
    : IDisposable
{
    public System.Drawing.Bitmap Bitmap { get; } = bitmap;

    public System.Drawing.Rectangle VirtualBounds { get; } = virtualBounds;

    public PhysicalSize Size { get; } = new(
        virtualBounds.Width,
        virtualBounds.Height);

    public void Dispose() => Bitmap.Dispose();
}
