using System.IO;
using System.Windows;
using ShotLens.Windows.App.Updates;
using ShotLens.Windows.Core.Updates;
using ShotLens.Windows.Core.Versioning;
using ShotLens.Windows.Platform.Updates;

namespace ShotLens.Windows.App;

public partial class MainWindow : Window
{
    private readonly UpdateCoordinator coordinator;

    public MainWindow()
    {
        InitializeComponent();
        var version = ProductVersion.Load(Path.Combine(AppContext.BaseDirectory, "VERSION"));
        VersionText.Text = $"版本 {version} · beta 通道";

        coordinator = new UpdateCoordinator(
            version,
            UpdateChannel.Beta,
            new GitHubReleaseSource(new HttpClient()),
            new InstallerDownloader(
                new HttpClient(),
                Path.Combine(Path.GetTempPath(), "ShotLens", "Updates")),
            new InstallerLauncher(new SystemProcessLauncher()));
        coordinator.StateChanged += RenderState;
        RenderState(coordinator.State);
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e) =>
        await coordinator.CheckAsync();

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (await coordinator.InstallAsync())
        {
            Application.Current.Shutdown();
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
}
