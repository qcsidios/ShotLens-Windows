namespace ShotLens.Windows.Core.Capture;

public sealed record MonitorDescriptor(
    string Id,
    string DeviceName,
    PhysicalRect DesktopBounds,
    PhysicalSize FrameSize,
    uint DpiX,
    uint DpiY,
    bool IsPrimary);
