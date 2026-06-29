namespace ShotLens.Windows.Platform.Capture;

public interface IMonitorCaptureFactory
{
    IReadOnlyList<MonitorCaptureTarget> EnumerateOutputs();

    IMonitorCaptureSession CreateSession(MonitorCaptureTarget target);
}
