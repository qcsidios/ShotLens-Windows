using System.IO;
using System.Net.Http;
using System.Windows;
using ShotLens.Windows.App.Capture;
using ShotLens.Windows.App.Configuration;
using ShotLens.Windows.App.Updates;
using ShotLens.Windows.App.Workflow;
using ShotLens.Windows.Core;
using ShotLens.Windows.Core.Settings;
using ShotLens.Windows.Core.Translation;
using ShotLens.Windows.Core.Updates;
using ShotLens.Windows.Core.Versioning;
using ShotLens.Windows.Platform.Ocr;
using ShotLens.Windows.Platform.Updates;

namespace ShotLens.Windows.App;

public partial class MainWindow : Window
{
    private readonly UpdateCoordinator coordinator;
    private readonly CaptureTranslateWorkflow workflow;
    private readonly SettingsStore settingsStore;
    private bool isCapturing;

    public MainWindow()
    {
        InitializeComponent();
        var version = ProductVersion.Load(Path.Combine(AppContext.BaseDirectory, "VERSION"));
        VersionText.Text = $"版本 {version} · beta 通道";
        settingsStore = new SettingsStore(SettingsPath());

        coordinator = new UpdateCoordinator(
            version,
            UpdateChannel.Beta,
            new GitHubReleaseSource(new HttpClient()),
            new InstallerDownloader(
                new HttpClient(),
                Path.Combine(Path.GetTempPath(), "ShotLens", "Updates")),
            new InstallerLauncher(new SystemProcessLauncher()));
        workflow = new CaptureTranslateWorkflow(
            new SelectionCaptureService(new DesktopCaptureService()),
            new OcrWorkerClient(
                new SystemOcrWorkerProcessFactory(),
                Path.Combine(
                    AppContext.BaseDirectory,
                    "ShotLens.Windows.Ocr.Worker.exe"),
                Path.Combine(Path.GetTempPath(), "ShotLens", "Ocr")),
            new TranslationService(new HttpClient()),
            settingsStore,
            DefaultApiKeyProvider.Load);
        coordinator.StateChanged += RenderState;
        RenderState(coordinator.State);
        RenderSettings(settingsStore.Load());
    }

    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (isCapturing)
        {
            return;
        }

        isCapturing = true;
        CaptureButton.IsEnabled = false;
        try
        {
            Hide();
            await Task.Delay(160);
            await workflow.RunAsync(
                message => WorkflowStatusText.Text = message,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            WorkflowStatusText.Text = $"截图翻译失败：{exception.Message}";
        }
        finally
        {
            Show();
            Activate();
            CaptureButton.IsEnabled = true;
            isCapturing = false;
        }
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e) =>
        await coordinator.CheckAsync();

    private void SaveApiButton_Click(object sender, RoutedEventArgs e)
    {
        var apiMode = CustomApiRadio.IsChecked == true
            ? ApiCredentialMode.Custom
            : DisabledApiRadio.IsChecked == true
                ? ApiCredentialMode.Disabled
                : ApiCredentialMode.DefaultFree;
        var settings = settingsStore.Load() with
        {
            Api = ApiConfiguration.Default with
            {
                Mode = apiMode,
                UserApiKey = string.IsNullOrWhiteSpace(UserApiKeyBox.Password)
                    ? null
                    : UserApiKeyBox.Password.Trim()
            }
        };
        settingsStore.Save(settings);
        RenderSettings(settings);
        WorkflowStatusText.Text = "API 设置已保存。";
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (await coordinator.InstallAsync())
        {
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void RenderState(UpdateState state)
    {
        CheckButton.IsEnabled = state.Kind is not (
            UpdateStateKind.Checking
            or UpdateStateKind.Downloading
            or UpdateStateKind.LaunchingInstaller);
        InstallButton.Visibility = state.Kind == UpdateStateKind.Available
            ? Visibility.Visible
            : Visibility.Collapsed;
        DownloadProgress.Visibility = state.Kind == UpdateStateKind.Downloading
            ? Visibility.Visible
            : Visibility.Collapsed;
        DownloadProgress.Value = state.Progress ?? 0;
        StatusText.Text = state.Kind switch
        {
            UpdateStateKind.Idle => "准备检查测试版本。",
            UpdateStateKind.Checking => "正在检查新版本…",
            UpdateStateKind.UpToDate => "当前已是最新测试版。",
            UpdateStateKind.Available => $"发现新版本 {state.Update!.Version}。",
            UpdateStateKind.Downloading => $"正在下载… {(state.Progress ?? 0):P0}",
            UpdateStateKind.LaunchingInstaller => "正在启动升级安装程序…",
            UpdateStateKind.Failed => state.Message ?? "升级失败。",
            _ => "未知状态。"
        };
    }

    private static string SettingsPath() =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            ProductIdentity.StableSettingsDirectoryName,
            "settings.json");

    private void RenderSettings(AppSettings settings)
    {
        DefaultApiRadio.IsChecked = settings.Api.Mode
            == ApiCredentialMode.DefaultFree;
        CustomApiRadio.IsChecked = settings.Api.Mode
            == ApiCredentialMode.Custom;
        DisabledApiRadio.IsChecked = settings.Api.Mode
            == ApiCredentialMode.Disabled;
        UserApiKeyBox.Password = settings.Api.UserApiKey ?? "";
        ApiStatusText.Text = DefaultApiKeyProvider.Load() is null
            ? "当前安装包未注入默认 Key；请填写自定义 Key 后使用。"
            : "默认限免 Key 已随安装包配置。";
    }
}
