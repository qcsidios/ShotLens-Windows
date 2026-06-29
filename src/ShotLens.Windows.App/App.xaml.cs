using System.IO;
using System.Windows;
using System.Windows.Threading;
using ShotLens.Windows.App.Capture;
using ShotLens.Windows.Core.Versioning;
using ShotLens.Windows.Platform.Capture;

namespace ShotLens.Windows.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--smoke", StringComparer.Ordinal))
        {
            _ = ProductVersion.Load(Path.Combine(AppContext.BaseDirectory, "VERSION"));
            Shutdown(0);
            return;
        }

        var captureArgumentIndex = Array.IndexOf(
            e.Args,
            "--capture-diagnostics");
        if (captureArgumentIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (captureArgumentIndex + 1 >= e.Args.Length)
            {
                Shutdown(2);
                return;
            }

            await RunCaptureDiagnosticsAsync(e.Args[captureArgumentIndex + 1]);
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        if (e.Args.Contains("--smoke-window", StringComparer.Ordinal))
        {
            _ = Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                () =>
                {
                    window.Close();
                    Shutdown(0);
                });
        }
    }

    private async Task RunCaptureDiagnosticsAsync(string outputDirectory)
    {
        string? fullOutputDirectory = null;
        try
        {
            fullOutputDirectory = Path.GetFullPath(outputDirectory);
            var runner = new CaptureDiagnosticRunner(
                new DxgiMonitorCaptureFactory(),
                new WpfPngFrameWriter());
            await Task.Run(
                () => runner.RunAsync(
                    fullOutputDirectory,
                    CancellationToken.None));
            Shutdown(0);
        }
        catch (Exception exception)
        {
            if (fullOutputDirectory is not null)
            {
                try
                {
                    Directory.CreateDirectory(fullOutputDirectory);
                    await File.WriteAllTextAsync(
                        Path.Combine(fullOutputDirectory, "error.txt"),
                        $"DXGI 截图诊断失败。{Environment.NewLine}"
                        + $"{exception.GetType().Name}: {exception.Message}");
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            Shutdown(1);
        }
    }
}
