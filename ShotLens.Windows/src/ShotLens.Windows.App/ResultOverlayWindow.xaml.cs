using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShotLens.Windows.Core;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using WpfClipboard = System.Windows.Clipboard;

namespace ShotLens.Windows.App;

public partial class ResultOverlayWindow : Window
{
    private readonly IReadOnlyList<TranslationResult> results;

    public ResultOverlayWindow(BitmapSource screenshot, IReadOnlyList<TranslationResult> results)
    {
        InitializeComponent();
        this.results = results;
        ScreenshotImage.Source = screenshot;
        Width = Math.Min(screenshot.PixelWidth + 40, SystemParameters.WorkArea.Width * 0.9);
        Height = Math.Min(screenshot.PixelHeight + 96, SystemParameters.WorkArea.Height * 0.9);
        TranslationCanvas.Width = screenshot.PixelWidth;
        TranslationCanvas.Height = screenshot.PixelHeight;
        SummaryTextBlock.Text = $"{results.Count} 个文本块";
        DrawTranslations();
    }

    private void CopyTextButton_Click(object sender, RoutedEventArgs e)
    {
        WpfClipboard.SetText(string.Join(Environment.NewLine, results.Select(result => result.Translation)));
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DrawTranslations()
    {
        foreach (var result in results)
        {
            var block = result.SourceBlock;
            var text = new TextBlock
            {
                Text = result.Translation,
                TextWrapping = TextWrapping.Wrap,
                Foreground = MediaBrushes.Black,
                FontWeight = FontWeights.SemiBold,
                FontSize = Math.Clamp(block.Height * 0.42, 10, 18)
            };

            var left = Math.Clamp(block.X, 0, Math.Max(0, TranslationCanvas.Width - 1));
            var top = Math.Clamp(block.Y, 0, Math.Max(0, TranslationCanvas.Height - 1));
            var maxWidth = Math.Max(32, TranslationCanvas.Width - left);
            var maxHeight = Math.Max(18, TranslationCanvas.Height - top);

            var border = new Border
            {
                Child = text,
                Background = new SolidColorBrush(MediaColor.FromArgb(224, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(MediaColor.FromArgb(190, 51, 161, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(3),
                Width = Math.Min(Math.Max(block.Width, 56), maxWidth),
                MaxHeight = maxHeight,
                MinHeight = Math.Min(Math.Max(block.Height, 18), maxHeight)
            };

            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top);
            TranslationCanvas.Children.Add(border);
        }
    }
}
