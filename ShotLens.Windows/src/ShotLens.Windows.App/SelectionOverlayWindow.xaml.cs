using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShotLens.Windows.App.Services;

namespace ShotLens.Windows.App;

public partial class SelectionOverlayWindow : Window
{
    private readonly CapturedImage screenshot;
    private readonly TaskCompletionSource<Rectangle?> completion = new();
    private System.Windows.Point? startPoint;

    private SelectionOverlayWindow(CapturedImage screenshot)
    {
        InitializeComponent();
        this.screenshot = screenshot;
        Left = screenshot.ScreenBounds.Left;
        Top = screenshot.ScreenBounds.Top;
        Width = screenshot.ScreenBounds.Width;
        Height = screenshot.ScreenBounds.Height;
        ScreenshotImage.Source = screenshot.ToImageSource();

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        KeyDown += OnKeyDown;
        Closed += (_, _) => completion.TrySetResult(null);
    }

    public static Task<Rectangle?> SelectAsync(CapturedImage screenshot)
    {
        var window = new SelectionOverlayWindow(screenshot);
        window.Show();
        window.Activate();
        return window.completion.Task;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        startPoint = e.GetPosition(OverlayCanvas);
        SelectionRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRectangle, startPoint.Value.X);
        Canvas.SetTop(SelectionRectangle, startPoint.Value.Y);
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (startPoint is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(OverlayCanvas);
        var x = Math.Min(startPoint.Value.X, current.X);
        var y = Math.Min(startPoint.Value.Y, current.Y);
        var width = Math.Abs(startPoint.Value.X - current.X);
        var height = Math.Abs(startPoint.Value.Y - current.Y);
        Canvas.SetLeft(SelectionRectangle, x);
        Canvas.SetTop(SelectionRectangle, y);
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (startPoint is null)
        {
            return;
        }

        ReleaseMouseCapture();
        var current = e.GetPosition(OverlayCanvas);
        var x = (int)Math.Round(Math.Min(startPoint.Value.X, current.X) + screenshot.ScreenBounds.Left);
        var y = (int)Math.Round(Math.Min(startPoint.Value.Y, current.Y) + screenshot.ScreenBounds.Top);
        var width = (int)Math.Round(Math.Abs(startPoint.Value.X - current.X));
        var height = (int)Math.Round(Math.Abs(startPoint.Value.Y - current.Y));

        completion.TrySetResult(width < 8 || height < 8 ? null : new Rectangle(x, y, width, height));
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        completion.TrySetResult(null);
        Close();
    }
}
