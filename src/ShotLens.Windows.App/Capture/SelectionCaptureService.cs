using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ShotLens.Windows.App.Capture;

public sealed class SelectionCaptureService(
    DesktopCaptureService desktopCaptureService)
{
    public SelectionCaptureResult? CaptureSelection()
    {
        using var snapshot = desktopCaptureService.Capture();
        using var form = new SelectionForm(snapshot);
        var selected = form.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? form.SelectedRectangle
            : null;
        if (selected is null)
        {
            return null;
        }

        using var cropped = snapshot.Bitmap.Clone(
            selected.Value,
            PixelFormat.Format32bppArgb);
        using var stream = new MemoryStream();
        cropped.Save(stream, ImageFormat.Png);
        return new SelectionCaptureResult(
            stream.ToArray(),
            new(selected.Value.Width, selected.Value.Height));
    }
}
