namespace ShotLens.Windows.Platform.Updates;

public interface IProcessLauncher
{
    void Start(string fileName, string arguments, bool useShellExecute);
}
