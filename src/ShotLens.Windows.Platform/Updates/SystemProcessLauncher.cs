using System.Diagnostics;

namespace ShotLens.Windows.Platform.Updates;

public sealed class SystemProcessLauncher : IProcessLauncher
{
    public void Start(string fileName, string arguments, bool useShellExecute)
    {
        Process.Start(new ProcessStartInfo(fileName)
        {
            Arguments = arguments,
            UseShellExecute = useShellExecute
        });
    }
}
