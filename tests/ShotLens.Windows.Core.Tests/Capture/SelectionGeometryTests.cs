using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Core.Tests.Capture;

public sealed class SelectionGeometryTests
{
    [Theory]
    [InlineData(96, 10.2, 20.2, 30.1, 40.1, 10, 20, 31, 41)]
    [InlineData(120, 10.2, 20.2, 30.1, 40.1, 12, 25, 39, 51)]
    [InlineData(144, 10.2, 20.2, 30.1, 40.1, 15, 30, 46, 61)]
    [InlineData(192, 10.2, 20.2, 30.1, 40.1, 20, 40, 61, 81)]
    public void Converts_logical_edges_outward_at_supported_dpi(
        uint dpi,
        double x,
        double y,
        double width,
        double height,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight)
    {
        var result = SelectionGeometry.ToFramePixels(
            new LogicalRect(x, y, width, height),
            dpi,
            dpi,
            new PhysicalSize(4000, 3000));

        Assert.Equal(
            new PhysicalRect(expectedX, expectedY, expectedWidth, expectedHeight),
            result);
    }

    [Fact]
    public void Clamps_to_frame_and_keeps_the_last_pixel()
    {
        var result = SelectionGeometry.ToFramePixels(
            new LogicalRect(90.1, 90.1, 20, 20),
            96,
            96,
            new PhysicalSize(100, 100));

        Assert.Equal(new PhysicalRect(90, 90, 10, 10), result);
        Assert.Equal(100, result.Right);
        Assert.Equal(100, result.Bottom);
    }

    [Fact]
    public void Converts_frame_pixels_to_negative_desktop_coordinates()
    {
        var monitor = new MonitorDescriptor(
            "left",
            "\\\\.\\DISPLAY2",
            new PhysicalRect(-2560, -200, 2560, 1440),
            new PhysicalSize(2560, 1440),
            120,
            120,
            false);

        var result = SelectionGeometry.ToDesktopPixels(
            new PhysicalRect(100, 50, 200, 80),
            monitor);

        Assert.Equal(new PhysicalRect(-2460, -150, 200, 80), result);
    }

    [Theory]
    [InlineData(19.99, 20, false)]
    [InlineData(20, 19.99, false)]
    [InlineData(20, 20, true)]
    public void Enforces_twenty_logical_pixel_minimum(
        double width,
        double height,
        bool expected) =>
        Assert.Equal(
            expected,
            SelectionGeometry.IsValidSelection(
                new LogicalRect(0, 0, width, height)));

    [Fact]
    public void Empty_clamped_selection_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SelectionGeometry.ToFramePixels(
                new LogicalRect(200, 200, 10, 10),
                96,
                96,
                new PhysicalSize(100, 100)));
    }
}
