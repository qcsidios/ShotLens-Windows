using System.Windows;
using ShotLens.Windows.App.Services;
using ShotLens.Windows.Core;

namespace ShotLens.Windows.App;

public partial class MainWindow : Window
{
    private readonly SettingsStore settingsStore = new();
    private readonly UpdateChecker updateChecker = new();
    private readonly TrayIconService trayIconService;
    private HotKeyService? hotKeyService;
    private ShotLensSettings settings;
    private bool isProcessing;

    public MainWindow()
    {
        InitializeComponent();
        settings = settingsStore.Load();
        trayIconService = new TrayIconService(
            showWindow: () => Dispatcher.Invoke(ShowMainWindow),
            startCapture: () => Dispatcher.Invoke(StartCaptureAsync),
            exit: () => Dispatcher.Invoke(Close));

        Loaded += OnLoaded;
        Closed += OnClosed;
        LoadSettingsIntoForm();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VersionTextBlock.Text = $"版本 {VersionInfo.Current}";
        trayIconService.Start();
        hotKeyService = new HotKeyService(this, StartCaptureAsync);
        hotKeyService.Register();
        StatusTextBlock.Text = "准备就绪。";
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        hotKeyService?.Dispose();
        trayIconService.Dispose();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsFromForm();
        StatusTextBlock.Text = "设置已保存。";
    }

    private async void TestApiButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsFromForm();
        StatusTextBlock.Text = "正在测试 API…";
        try
        {
            var translator = new OpenAITranslator(settings.ToTranslationSettings());
            await translator.ValidateConnectivityAsync();
            StatusTextBlock.Text = "API 可用。";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"API 测试失败：{ex.Message}";
        }
    }

    private void StartCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        StartCaptureAsync();
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "正在检查 GitHub Release…";
        try
        {
            var result = await updateChecker.CheckAsync();
            StatusTextBlock.Text = result.ReleaseUrl is null
                ? result.Message
                : $"{result.Message}：{result.ReleaseUrl}";
            if (result.HasUpdate && result.ReleaseUrl is not null)
            {
                UpdateChecker.OpenReleasePage(result.ReleaseUrl);
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"检查更新失败：{ex.Message}";
        }
    }

    private async void StartCaptureAsync()
    {
        if (isProcessing)
        {
            return;
        }

        SaveSettingsFromForm();
        if (!settings.ToTranslationSettings().IsConfigured)
        {
            ShowMainWindow();
            StatusTextBlock.Text = "请先配置翻译 API。";
            return;
        }

        isProcessing = true;
        StatusTextBlock.Text = "请选择要翻译的区域…";
        try
        {
            Hide();
            await Task.Delay(160);

            using var screenshot = ScreenCaptureService.CaptureVirtualScreen();
            var selection = await SelectionOverlayWindow.SelectAsync(screenshot);
            if (selection is null)
            {
                ShowMainWindow();
                StatusTextBlock.Text = "已取消截图。";
                return;
            }

            using var cropped = ScreenCaptureService.Crop(screenshot, selection.Value);
            var ocrBlocks = await WindowsOcrService.RecognizeAsync(cropped.Bitmap);
            if (ocrBlocks.Count == 0)
            {
                ShowMainWindow();
                StatusTextBlock.Text = "没有识别到文字。";
                return;
            }

            var translator = new OpenAITranslator(settings.ToTranslationSettings());
            var translations = await translator.TranslateAsync(
                ocrBlocks.Select(block => block.Text).ToArray(),
                "auto",
                "zh-Hans");
            var results = ocrBlocks
                .Zip(translations, (block, translation) => new TranslationResult(block, translation))
                .ToArray();

            var overlay = new ResultOverlayWindow(cropped.ToImageSource(), results)
            {
                Left = selection.Value.X,
                Top = selection.Value.Y
            };
            overlay.Show();

            StatusTextBlock.Text = $"已翻译 {results.Length} 个文本块。";
        }
        catch (Exception ex)
        {
            ShowMainWindow();
            StatusTextBlock.Text = $"截图翻译失败：{ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void LoadSettingsIntoForm()
    {
        EndpointTextBox.Text = settings.ApiEndpoint;
        ApiKeyPasswordBox.Password = settings.ApiKey;
        ModelTextBox.Text = settings.Model;
        UseDefaultCheckBox.IsChecked = settings.DefaultFallbackEnabled;
    }

    private void SaveSettingsFromForm()
    {
        settings = new ShotLensSettings(
            EndpointTextBox.Text.Trim(),
            ApiKeyPasswordBox.Password.Trim(),
            ModelTextBox.Text.Trim(),
            UseDefaultCheckBox.IsChecked ?? true);
        settingsStore.Save(settings);
    }
}
