namespace ShotLens.Windows.Core.Capture;

public static class SelectionGeometry
{
    private const double MinimumLogicalSize = 20;

    public static bool IsValidSelection(LogicalRect selection) =>
        double.IsFinite(selection.Width)
        && double.IsFinite(selection.Height)
        && selection.Width >= MinimumLogicalSize
        && selection.Height >= MinimumLogicalSize;

    public static PhysicalRect ToFramePixels(
        LogicalRect selection,
        uint dpiX,
        uint dpiY,
        PhysicalSize frameSize)
    {
        if (dpiX == 0 || dpiY == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiX));
        }

        if (frameSize.Width <= 0 || frameSize.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameSize));
        }

        var scaleX = dpiX / 96d;
        var scaleY = dpiY / 96d;
        var left = Math.Clamp((int)Math.Floor(selection.X * scaleX), 0, frameSize.Width);
        var top = Math.Clamp((int)Math.Floor(selection.Y * scaleY), 0, frameSize.Height);
        var right = Math.Clamp(
            (int)Math.Ceiling((selection.X + selection.Width) * scaleX),
            0,
            frameSize.Width);
        var bottom = Math.Clamp(
            (int)Math.Ceiling((selection.Y + selection.Height) * scaleY),
            0,
            frameSize.Height);

        if (right <= left || bottom <= top)
        {
            throw new ArgumentOutOfRangeException(nameof(selection));
        }

        return new PhysicalRect(left, top, right - left, bottom - top);
    }

    public static PhysicalRect ToDesktopPixels(
        PhysicalRect framePixels,
        MonitorDescriptor monitor) =>
        new(
            checked(monitor.DesktopBounds.X + framePixels.X),
            checked(monitor.DesktopBounds.Y + framePixels.Y),
            framePixels.Width,
            framePixels.Height);
}
