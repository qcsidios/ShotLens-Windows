namespace ShotLens.Windows.Platform.Capture;

public interface IMonitorCaptureSession : IDisposable
{
    ValueTask<CapturedBgraFrame> AcquireFrameAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
