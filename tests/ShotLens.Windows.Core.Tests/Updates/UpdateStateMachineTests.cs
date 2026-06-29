using ShotLens.Windows.Core.Updates;
using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.Core.Tests.Updates;

public sealed class UpdateStateMachineTests
{
    [Fact]
    public void Repeated_check_is_rejected_while_checking()
    {
        var machine = new UpdateStateMachine();

        Assert.True(machine.TryBeginCheck());
        Assert.False(machine.TryBeginCheck());
        Assert.Equal(UpdateStateKind.Checking, machine.State.Kind);
    }

    [Fact]
    public void Download_requires_an_available_update()
    {
        var machine = new UpdateStateMachine();

        Assert.False(machine.TryBeginDownload());
        machine.TryBeginCheck();
        machine.SetAvailable(BetaUpdate());

        Assert.True(machine.TryBeginDownload());
        Assert.Equal(UpdateStateKind.Downloading, machine.State.Kind);
    }

    [Fact]
    public void Progress_is_clamped_and_launch_follows_download()
    {
        var machine = AvailableMachine();
        machine.TryBeginDownload();

        machine.SetDownloadProgress(1.5);
        Assert.Equal(1, machine.State.Progress);

        machine.SetLaunchingInstaller();
        Assert.Equal(UpdateStateKind.LaunchingInstaller, machine.State.Kind);
    }

    [Fact]
    public void Failure_can_be_retried_by_starting_a_new_check()
    {
        var machine = new UpdateStateMachine();
        machine.SetFailed("网络失败");

        Assert.Equal(UpdateStateKind.Failed, machine.State.Kind);
        Assert.True(machine.TryBeginCheck());
        Assert.Equal(UpdateStateKind.Checking, machine.State.Kind);
    }

    private static UpdateStateMachine AvailableMachine()
    {
        var machine = new UpdateStateMachine();
        machine.TryBeginCheck();
        machine.SetAvailable(BetaUpdate());
        return machine;
    }

    private static AvailableUpdate BetaUpdate() =>
        new(
            SemanticVersion.Parse("v0.2.0-beta.2"),
            new Uri("https://github.test/release"),
            new GitHubAsset(
                "ShotLens-Beta-v0.2.0-beta.2-Setup.exe",
                new Uri("https://github.test/setup.exe")));
}
