using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace ShotLens.Windows.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                WriteCrashLog(exception);
            }
        };

        _ = SetProcessDpiAwarenessContext(new IntPtr(-4));
        base.OnStartup(e);

        if (e.Args.Contains("--smoke", StringComparer.OrdinalIgnoreCase))
        {
            RunSmokeCheck();
            Shutdown(0);
            return;
        }

        if (e.Args.Contains("--smoke-window", StringComparer.OrdinalIgnoreCase))
        {
            RunSmokeCheck();
            var smokeWindow = new MainWindow();
            smokeWindow.Show();
            smokeWindow.Close();
            Shutdown(0);
            return;
        }

        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            WriteCrashLog(ex);
            MessageBox.Show(
                $"ShotLens 启动失败，错误已写入：{CrashLogPath}\n\n{ex.Message}",
                "ShotLens",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
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

    internal static void WriteCrashLog(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
            File.AppendAllText(
                CrashLogPath,
                $"[{DateTimeOffset.Now:O}] {exception}\n\n");
        }
        catch
        {
            // 如果日志本身写失败，避免二次崩溃。
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception);
        MessageBox.Show(
            $"ShotLens 运行异常，错误已写入：{CrashLogPath}\n\n{e.Exception.Message}",
            "ShotLens",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static string CrashLogPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShotLens",
            "crash.log");
}
