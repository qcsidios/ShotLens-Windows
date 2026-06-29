using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Platform.Capture;

public interface IPngFrameWriter
{
    ValueTask<PhysicalSize> WriteAsync(
        string path,
        CapturedBgraFrame frame,
        CancellationToken cancellationToken);
}
