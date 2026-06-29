namespace ShotLens.Windows.Platform.Updates;

public sealed class InstallerLauncher(IProcessLauncher processLauncher)
{
    private const string Arguments =
        "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS";

    public void Launch(string installerPath) =>
        processLauncher.Start(installerPath, Arguments, useShellExecute: true);
}
