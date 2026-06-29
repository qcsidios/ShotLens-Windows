using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Imaging;

namespace ShotLens.Windows.Platform.Tests.Capture;

public sealed class PngDimensionsTests
{
    [Fact]
    public void Reads_big_endian_dimensions_from_ihdr()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(
                path,
                [
                    137, 80, 78, 71, 13, 10, 26, 10,
                    0, 0, 0, 13,
                    73, 72, 68, 82,
                    0, 0, 7, 128,
                    0, 0, 4, 56
                ]);

            Assert.Equal(
                new PhysicalSize(1920, 1080),
                PngDimensions.Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Rejects_a_non_png_file()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not a png");

            Assert.Throws<InvalidDataException>(
                () => PngDimensions.Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
