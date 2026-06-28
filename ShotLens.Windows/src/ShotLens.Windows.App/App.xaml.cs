using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ShotLens.Windows.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        _ = SetProcessDpiAwarenessContext(new IntPtr(-4));
        base.OnStartup(e);

        if (e.Args.Contains("--smoke", StringComparer.OrdinalIgnoreCase))
        {
            RunSmokeCheck();
            Shutdown(0);
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    private static void RunSmokeCheck()
    {
        var versionPath = Path.Combine(AppContext.BaseDirectory, "VERSION");
        if (!File.Exists(versionPath))
        {
            throw new FileNotFoundException("VERSION file is missing from publish output.", versionPath);
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "ShotLens.ico");
        if (!File.Exists(iconPath))
        {
            throw new FileNotFoundException("ShotLens.ico is missing from publish output.", iconPath);
        }

        var logo = new BitmapImage();
        logo.BeginInit();
        logo.UriSource = new Uri("pack://application:,,,/Resources/ShotLens.png", UriKind.Absolute);
        logo.CacheOption = BitmapCacheOption.OnLoad;
        logo.EndInit();
        _ = logo.PixelWidth;
    }
}
