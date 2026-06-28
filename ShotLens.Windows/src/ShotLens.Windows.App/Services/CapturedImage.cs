using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace ShotLens.Windows.App.Services;

public sealed class CapturedImage : IDisposable
{
    public CapturedImage(Bitmap bitmap, Rectangle screenBounds)
    {
        Bitmap = bitmap;
        ScreenBounds = screenBounds;
    }

    public Bitmap Bitmap { get; }

    public Rectangle ScreenBounds { get; }

    public BitmapSource ToImageSource()
    {
        using var stream = new MemoryStream();
        Bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public void Dispose()
    {
        Bitmap.Dispose();
    }
}
