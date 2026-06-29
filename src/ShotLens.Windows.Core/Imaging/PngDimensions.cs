using System.Buffers.Binary;
using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Core.Imaging;

public static class PngDimensions
{
    private static readonly byte[] HeaderPrefix =
    [
        137, 80, 78, 71, 13, 10, 26, 10,
        0, 0, 0, 13,
        73, 72, 68, 82
    ];

    public static PhysicalSize Read(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(path);
        try
        {
            stream.ReadExactly(header);
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("PNG 文件头不完整。", exception);
        }
        if (!header[..HeaderPrefix.Length].SequenceEqual(HeaderPrefix))
        {
            throw new InvalidDataException("文件不包含有效的 PNG IHDR。");
        }

        var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
        var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("PNG 尺寸无效。");
        }

        return new PhysicalSize(width, height);
    }
}
