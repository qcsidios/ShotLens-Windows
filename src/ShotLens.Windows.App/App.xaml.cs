using System.IO;
using System.Windows;
using System.Windows.Threading;
using ShotLens.Windows.Core.Versioning;

namespace ShotLens.Windows.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--smoke", StringComparer.Ordinal))
        {
            _ = ProductVersion.Load(Path.Combine(AppContext.BaseDirectory, "VERSION"));
            Shutdown(0);
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        if (e.Args.Contains("--smoke-window", StringComparer.Ordinal))
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                () =>
                {
                    window.Close();
                    Shutdown(0);
                });
        }
    }
}
