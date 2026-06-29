using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Platform.Capture;

namespace ShotLens.Windows.App.Capture;

public sealed class WpfPngFrameWriter : IPngFrameWriter
{
    public ValueTask<PhysicalSize> WriteAsync(
        string path,
        CapturedBgraFrame frame,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var partialPath = $"{path}.partial";
        try
        {
            var bitmap = BitmapSource.Create(
                frame.Size.Width,
                frame.Size.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                frame.Pixels.ToArray(),
                frame.RowPitch);
            bitmap.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = new FileStream(
                partialPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                encoder.Save(stream);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partialPath, path, overwrite: true);
            return ValueTask.FromResult(PngDimensions.Read(path));
        }
        finally
        {
            if (File.Exists(partialPath))
            {
                File.Delete(partialPath);
            }
        }
    }
}
