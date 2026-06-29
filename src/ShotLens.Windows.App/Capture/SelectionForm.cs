using System.Drawing;
using System.Windows.Forms;

namespace ShotLens.Windows.App.Capture;

public sealed class SelectionForm : Form
{
    private const int MinimumSelectionSize = 20;
    private readonly DesktopSnapshot _snapshot;
    private Point? _start;
    private Rectangle? _current;

    public SelectionForm(DesktopSnapshot snapshot)
    {
        _snapshot = snapshot;
        Bounds = snapshot.VirtualBounds;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        KeyPreview = true;
    }

    public Rectangle? SelectedRectangle { get; private set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(_snapshot.Bitmap, 0, 0);
        using var overlay = new SolidBrush(Color.FromArgb(96, 0, 0, 0));
        e.Graphics.FillRectangle(overlay, ClientRectangle);

        if (_current is { } rectangle)
        {
            e.Graphics.DrawImage(
                _snapshot.Bitmap,
                rectangle,
                rectangle,
                GraphicsUnit.Pixel);
            using var pen = new Pen(Color.White, 2);
            e.Graphics.DrawRectangle(pen, rectangle);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _start = e.Location;
        _current = new Rectangle(e.Location, Size.Empty);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_start is null)
        {
            return;
        }

        _current = Normalize(_start.Value, e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_start is null || e.Button != MouseButtons.Left)
        {
            return;
        }

        var rectangle = Normalize(_start.Value, e.Location);
        if (rectangle.Width < MinimumSelectionSize
            || rectangle.Height < MinimumSelectionSize)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        SelectedRectangle = rectangle;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private static Rectangle Normalize(Point start, Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var right = Math.Max(start.X, end.X);
        var bottom = Math.Max(start.Y, end.Y);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }
}
