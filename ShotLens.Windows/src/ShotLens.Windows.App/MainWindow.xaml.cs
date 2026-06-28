using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShotLens.Windows.App.Services;
using ShotLens.Windows.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ShotLens.Windows.App;

public partial class MainWindow : Window
{
    private readonly SettingsStore settingsStore = new();
    private readonly UpdateChecker updateChecker = new();
    private readonly TrayIconService trayIconService;
    private HotKeyService? hotKeyService;
    private ShotLensSettings settings;
    private bool isProcessing;
    private bool isRecordingShortcut;

    public MainWindow()
    {
        InitializeComponent();
        Icon = LoadWindowIcon();
        LogoImage.Source = LoadLogoImage();
        settings = settingsStore.Load();
        trayIconService = new TrayIconService(
            showWindow: () => Dispatcher.Invoke(ShowMainWindow),
            startCapture: () => Dispatcher.Invoke(StartCaptureAsync),
            exit: () => Dispatcher.Invoke(Close));

        Loaded += OnLoaded;
        Closed += OnClosed;
        PreviewKeyDown += OnPreviewKeyDown;
        LoadSettingsIntoForm();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VersionTextBlock.Text = $"版本 {VersionInfo.Current}";
        try
        {
            trayIconService.Start();
            hotKeyService = new HotKeyService(this, settings.Shortcut, StartCaptureAsync);
            if (TryRegisterHotKey())
            {
                StatusTextBlock.Text = "准备就绪。";
            }
        }
        catch (Exception ex)
        {
            App.WriteCrashLog(ex);
            StatusTextBlock.Text = $"部分启动项失败：{ex.Message}";
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        try
        {
            hotKeyService?.Dispose();
            trayIconService.Dispose();
        }
        catch (Exception ex)
        {
            App.WriteCrashLog(ex);
        }
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
            StatusTextBlock.Text = result.Message;
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
        ShortcutTextBlock.Text = settings.Shortcut.DisplayText;
    }

    private void SaveSettingsFromForm()
    {
        settings = new ShotLensSettings
        {
            ApiEndpoint = EndpointTextBox.Text.Trim(),
            ApiKey = ApiKeyPasswordBox.Password.Trim(),
            Model = ModelTextBox.Text.Trim(),
            DefaultFallbackEnabled = UseDefaultCheckBox.IsChecked ?? true,
            Shortcut = settings.Shortcut
        };
        settingsStore.Save(settings);
    }

    private void RecordShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        isRecordingShortcut = true;
        RecordShortcutButton.Content = "录制中";
        ShortcutTextBlock.Text = "请按新的快捷键";
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!isRecordingShortcut)
        {
            return;
        }

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        try
        {
            var shortcut = ShortcutGesture.FromInput(key, Keyboard.Modifiers);
            settings = new ShotLensSettings
            {
                ApiEndpoint = EndpointTextBox.Text.Trim(),
                ApiKey = ApiKeyPasswordBox.Password.Trim(),
                Model = ModelTextBox.Text.Trim(),
                DefaultFallbackEnabled = UseDefaultCheckBox.IsChecked ?? true,
                Shortcut = shortcut
            };
            hotKeyService?.Update(shortcut);
            settingsStore.Save(settings);
            ShortcutTextBlock.Text = shortcut.DisplayText;
            StatusTextBlock.Text = "快捷键已更新。";
        }
        catch (Exception ex)
        {
            ShortcutTextBlock.Text = settings.Shortcut.DisplayText;
            StatusTextBlock.Text = ex.Message;
        }
        finally
        {
            isRecordingShortcut = false;
            RecordShortcutButton.Content = "设置";
        }
    }

    private bool TryRegisterHotKey()
    {
        try
        {
            hotKeyService?.Register();
            return true;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
            return false;
        }
    }

    private static ImageSource? LoadLogoImage()
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri("pack://application:,,,/Resources/ShotLens.png", UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static ImageSource? LoadWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "ShotLens.ico");
        if (!File.Exists(iconPath))
        {
            return null;
        }

        var frame = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        frame.Freeze();
        return frame;
    }
}
