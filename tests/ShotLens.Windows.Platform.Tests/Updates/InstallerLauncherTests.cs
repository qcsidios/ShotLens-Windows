using ShotLens.Windows.Platform.Updates;

namespace ShotLens.Windows.Platform.Tests.Updates;

public sealed class InstallerLauncherTests
{
    [Fact]
    public void Launches_installer_with_silent_close_and_restart_arguments()
    {
        var processLauncher = new RecordingProcessLauncher();
        var launcher = new InstallerLauncher(processLauncher);

        launcher.Launch(@"C:\Temp\ShotLens-Beta-v0.2.0-beta.2-Setup.exe");

        Assert.Equal(
            @"C:\Temp\ShotLens-Beta-v0.2.0-beta.2-Setup.exe",
            processLauncher.FileName);
        Assert.Equal(
            "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            processLauncher.Arguments);
        Assert.True(processLauncher.UseShellExecute);
    }

    private sealed class RecordingProcessLauncher : IProcessLauncher
    {
        public string FileName { get; private set; } = "";

        public string Arguments { get; private set; } = "";

        public bool UseShellExecute { get; private set; }

        public void Start(string fileName, string arguments, bool useShellExecute)
        {
            FileName = fileName;
            Arguments = arguments;
            UseShellExecute = useShellExecute;
        }
    }
}
