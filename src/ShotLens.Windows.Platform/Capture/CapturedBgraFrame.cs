using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Platform.Capture;

public sealed class CapturedBgraFrame : IDisposable
{
    public CapturedBgraFrame(
        PhysicalSize size,
        int rowPitch,
        byte[] pixels)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "帧尺寸必须为正数。");
        }

        if (rowPitch < checked(size.Width * 4))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rowPitch),
                "BGRA 行跨度不能小于宽度乘以四。");
        }

        if (pixels.Length < checked(rowPitch * size.Height))
        {
            throw new ArgumentException("像素缓冲区不足以容纳整帧。", nameof(pixels));
        }

        Size = size;
        RowPitch = rowPitch;
        Pixels = pixels;
    }

    public PhysicalSize Size { get; }

    public int RowPitch { get; }

    public ReadOnlyMemory<byte> Pixels { get; }

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}
