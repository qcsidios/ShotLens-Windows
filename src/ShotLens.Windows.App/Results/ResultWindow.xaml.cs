using System.Windows;

namespace ShotLens.Windows.App.Results;

public partial class ResultWindow : Window
{
    public ResultWindow(
        string originalText,
        string translatedText,
        string status)
    {
        InitializeComponent();
        OriginalText.Text = originalText;
        TranslatedText.Text = translatedText;
        StatusText.Text = status;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TranslatedText.Text))
        {
            System.Windows.Clipboard.SetText(TranslatedText.Text);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}
