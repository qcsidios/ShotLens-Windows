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
                FontSize = Math.Clamp(block.Height * 0.58, 11, 24)
            };

            var border = new Border
            {
                Child = text,
                Background = new SolidColorBrush(MediaColor.FromArgb(224, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(MediaColor.FromArgb(190, 51, 161, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(3),
                Width = Math.Max(block.Width, 56),
                MinHeight = Math.Max(block.Height, 18)
            };

            Canvas.SetLeft(border, block.X);
            Canvas.SetTop(border, block.Y);
            TranslationCanvas.Children.Add(border);
        }
    }
}
