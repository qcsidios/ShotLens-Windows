using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Platform.Capture;

public static class BgraFrameRotation
{
    public static CapturedBgraFrame ToDisplayOrientation(
        CapturedBgraFrame source,
        MonitorRotation rotation)
    {
        var destinationSize = rotation is MonitorRotation.Rotate90
            or MonitorRotation.Rotate270
            ? new PhysicalSize(source.Size.Height, source.Size.Width)
            : source.Size;
        var destinationRowPitch = rotation is MonitorRotation.Identity
            ? source.RowPitch
            : checked(destinationSize.Width * 4);
        var destination = new byte[
            checked(destinationRowPitch * destinationSize.Height)];
        var sourcePixels = source.Pixels.Span;

        for (var sourceY = 0; sourceY < source.Size.Height; sourceY++)
        {
            for (var sourceX = 0; sourceX < source.Size.Width; sourceX++)
            {
                var (destinationX, destinationY) = rotation switch
                {
                    MonitorRotation.Identity => (sourceX, sourceY),
                    MonitorRotation.Rotate90 =>
                        (source.Size.Height - 1 - sourceY, sourceX),
                    MonitorRotation.Rotate180 =>
                        (source.Size.Width - 1 - sourceX,
                            source.Size.Height - 1 - sourceY),
                    MonitorRotation.Rotate270 =>
                        (sourceY, source.Size.Width - 1 - sourceX),
                    _ => throw new ArgumentOutOfRangeException(nameof(rotation))
                };
                var sourceOffset = checked(sourceY * source.RowPitch + sourceX * 4);
                var destinationOffset = checked(
                    destinationY * destinationRowPitch + destinationX * 4);
                sourcePixels
                    .Slice(sourceOffset, 4)
                    .CopyTo(destination.AsSpan(destinationOffset, 4));
            }
        }

        return new CapturedBgraFrame(
            destinationSize,
            destinationRowPitch,
            destination);
    }
}
