using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Platform.Capture;

public sealed record MonitorCaptureTarget(
    int AdapterIndex,
    int OutputIndex,
    MonitorDescriptor Monitor,
    MonitorRotation Rotation = MonitorRotation.Identity);
