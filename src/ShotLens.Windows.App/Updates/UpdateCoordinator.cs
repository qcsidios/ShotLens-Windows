using ShotLens.Windows.Core.Updates;
using ShotLens.Windows.Core.Versioning;
using ShotLens.Windows.Platform.Updates;

namespace ShotLens.Windows.App.Updates;

public sealed class UpdateCoordinator(
    SemanticVersion currentVersion,
    UpdateChannel channel,
    IGitHubReleaseSource releaseSource,
    InstallerDownloader downloader,
    InstallerLauncher installerLauncher)
{
    private readonly UpdateStateMachine stateMachine = new();

    public event Action<UpdateState>? StateChanged;

    public UpdateState State => stateMachine.State;

    public async Task CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!stateMachine.TryBeginCheck())
        {
            return;
        }

        Publish();
        try
        {
            var releases = await releaseSource.GetReleasesAsync(cancellationToken);
            var update = ReleaseSelector.SelectLatest(currentVersion, channel, releases);
            if (update is null)
            {
                stateMachine.SetUpToDate();
            }
            else
            {
                stateMachine.SetAvailable(update);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stateMachine.SetFailed("检查已取消。");
        }
        catch (Exception)
        {
            stateMachine.SetFailed("无法连接更新服务器。");
        }

        Publish();
    }

    public async Task<bool> InstallAsync(CancellationToken cancellationToken = default)
    {
        if (!stateMachine.TryBeginDownload())
        {
            return false;
        }

        Publish();
        try
        {
            var progress = new Progress<double>(value =>
            {
                stateMachine.SetDownloadProgress(value);
                Publish();
            });
            var path = await downloader.DownloadAsync(
                stateMachine.State.Update!,
                progress,
                cancellationToken);
            stateMachine.SetLaunchingInstaller();
            Publish();
            installerLauncher.Launch(path);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stateMachine.SetFailed("下载已取消。");
        }
        catch (Exception)
        {
            stateMachine.SetFailed("升级失败，请稍后重试。");
        }

        Publish();
        return false;
    }

    private void Publish() => StateChanged?.Invoke(stateMachine.State);
}
