using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.App.Capture;

public sealed record SelectionCaptureResult(
    byte[] PngBytes,
    PhysicalSize Size);
