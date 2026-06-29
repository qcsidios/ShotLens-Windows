using System.Text.Json;

namespace ShotLens.Windows.Platform.Capture;

public sealed class CaptureDiagnosticRunner(
    IMonitorCaptureFactory captureFactory,
    IPngFrameWriter pngWriter)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<CaptureDiagnosticManifest> RunAsync(
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var outputs = new List<CaptureDiagnosticOutput>();
        foreach (var target in captureFactory.EnumerateOutputs())
        {
            cancellationToken.ThrowIfCancellationRequested();
            outputs.Add(
                await CaptureOutputAsync(
                    outputDirectory,
                    target,
                    cancellationToken));
        }

        var manifest = new CaptureDiagnosticManifest(
            1,
            DateTimeOffset.UtcNow,
            outputs);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, JsonOptions),
            cancellationToken);
        return manifest;
    }

    private async Task<CaptureDiagnosticOutput> CaptureOutputAsync(
        string outputDirectory,
        MonitorCaptureTarget target,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var session = captureFactory.CreateSession(target);
            try
            {
                using var frame = await session.AcquireFrameAsync(
                    TimeSpan.FromMilliseconds(500),
                    cancellationToken);
                var fileName = $"{SafeFileName(target.Monitor.Id)}.png";
                var writtenSize = await pngWriter.WriteAsync(
                    Path.Combine(outputDirectory, fileName),
                    frame,
                    cancellationToken);

                return writtenSize == frame.Size
                    ? Output(target, fileName, "captured", null)
                    : Output(
                        target,
                        null,
                        "invalid-png-size",
                        "PNG 尺寸与物理帧尺寸不一致。");
            }
            catch (CaptureAccessLostException) when (attempt == 0)
            {
            }
            catch (CaptureAccessLostException exception)
            {
                return Output(target, null, "access-lost", exception.Message);
            }
            catch (CaptureTimeoutException exception)
            {
                return Output(target, null, "timed-out", exception.Message);
            }
            catch (IOException exception)
            {
                return Output(target, null, "write-failed", exception.Message);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return Output(target, null, "capture-failed", exception.Message);
            }
        }

        throw new InvalidOperationException("不可达的截图重试状态。");
    }

    private static CaptureDiagnosticOutput Output(
        MonitorCaptureTarget target,
        string? fileName,
        string status,
        string? error) =>
        new(
            target.Monitor.Id,
            target.Monitor.DeviceName,
            target.AdapterIndex,
            target.OutputIndex,
            target.Monitor.DesktopBounds.X,
            target.Monitor.DesktopBounds.Y,
            target.Monitor.FrameSize.Width,
            target.Monitor.FrameSize.Height,
            target.Monitor.DpiX,
            target.Monitor.DpiY,
            target.Monitor.IsPrimary,
            fileName,
            status,
            error);

    private static string SafeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(
            value.Select(
                character => invalidCharacters.Contains(character)
                    ? '-'
                    : character));
    }
}
