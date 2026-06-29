using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Platform.Capture;

namespace ShotLens.Windows.Platform.Tests.Capture;

public sealed class BgraFrameRotationTests
{
    [Fact]
    public void Rotates_bgra_pixels_clockwise_without_scaling()
    {
        using var source = new CapturedBgraFrame(
            new PhysicalSize(2, 1),
            8,
            [
                1, 2, 3, 4,
                5, 6, 7, 8
            ]);

        using var result = BgraFrameRotation.ToDisplayOrientation(
            source,
            MonitorRotation.Rotate90);

        Assert.Equal(new PhysicalSize(1, 2), result.Size);
        Assert.Equal(4, result.RowPitch);
        Assert.Equal(
            [
                1, 2, 3, 4,
                5, 6, 7, 8
            ],
            result.Pixels.ToArray());
    }

    [Fact]
    public void Preserves_row_padding_when_rotation_is_identity()
    {
        using var source = new CapturedBgraFrame(
            new PhysicalSize(1, 1),
            8,
            [1, 2, 3, 4, 0, 0, 0, 0]);

        using var result = BgraFrameRotation.ToDisplayOrientation(
            source,
            MonitorRotation.Identity);

        Assert.Equal(source.Size, result.Size);
        Assert.Equal(source.RowPitch, result.RowPitch);
        Assert.Equal(source.Pixels.ToArray(), result.Pixels.ToArray());
    }
}
