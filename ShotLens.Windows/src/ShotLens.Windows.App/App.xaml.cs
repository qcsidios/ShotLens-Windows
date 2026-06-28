using System.Runtime.InteropServices;
using System.Windows;

namespace ShotLens.Windows.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        _ = SetProcessDpiAwarenessContext(new IntPtr(-4));
        base.OnStartup(e);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);
}
