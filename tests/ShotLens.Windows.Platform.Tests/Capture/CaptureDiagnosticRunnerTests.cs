using System.Text.Json;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Platform.Capture;

namespace ShotLens.Windows.Platform.Tests.Capture;

public sealed class CaptureDiagnosticRunnerTests : IDisposable
{
    private readonly string _outputDirectory =
        Path.Combine(Path.GetTempPath(), $"shotlens-capture-{Guid.NewGuid():N}");

    [Fact]
    public async Task Writes_one_unscaled_png_per_output_and_a_manifest()
    {
        var factory = new FakeCaptureFactory(
            Target("display-1", 0, 0, 1920, 1080),
            Target("display-2", 0, 1, 2560, 1440));
        var writer = new FakePngWriter();
        var runner = new CaptureDiagnosticRunner(factory, writer);

        await runner.RunAsync(_outputDirectory, CancellationToken.None);

        Assert.Equal(
            ["display-1.png", "display-2.png"],
            writer.WrittenFiles.Select(Path.GetFileName));
        using var manifest = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(_outputDirectory, "manifest.json")));
        var outputs = manifest.RootElement.GetProperty("outputs");
        Assert.Equal(2, outputs.GetArrayLength());
        Assert.All(
            outputs.EnumerateArray(),
            output => Assert.Equal(
                "captured",
                output.GetProperty("status").GetString()));
        Assert.All(factory.Sessions, session => Assert.True(session.IsDisposed));
        Assert.All(factory.Frames, frame => Assert.True(frame.IsDisposed));
    }

    [Fact]
    public async Task Records_timeout_and_releases_the_session()
    {
        var factory = new FakeCaptureFactory(Target("display-1", 0, 0, 1920, 1080))
        {
            AcquireException = new CaptureTimeoutException()
        };
        var runner = new CaptureDiagnosticRunner(factory, new FakePngWriter());

        await runner.RunAsync(_outputDirectory, CancellationToken.None);

        Assert.Equal("timed-out", await ReadFirstStatusAsync());
        Assert.True(factory.Sessions.Single().IsDisposed);
    }

    [Fact]
    public async Task Rebuilds_once_after_access_lost()
    {
        var factory = new FakeCaptureFactory(Target("display-1", 0, 0, 1920, 1080))
        {
            FailFirstAcquireWithAccessLost = true
        };
        var runner = new CaptureDiagnosticRunner(factory, new FakePngWriter());

        await runner.RunAsync(_outputDirectory, CancellationToken.None);

        Assert.Equal("captured", await ReadFirstStatusAsync());
        Assert.Equal(2, factory.Sessions.Count);
        Assert.All(factory.Sessions, session => Assert.True(session.IsDisposed));
    }

    [Fact]
    public async Task Records_png_write_failure_and_releases_the_frame()
    {
        var factory = new FakeCaptureFactory(Target("display-1", 0, 0, 1920, 1080));
        var writer = new FakePngWriter
        {
            Exception = new IOException("磁盘已满")
        };
        var runner = new CaptureDiagnosticRunner(factory, writer);

        await runner.RunAsync(_outputDirectory, CancellationToken.None);

        Assert.Equal("write-failed", await ReadFirstStatusAsync());
        Assert.True(factory.Frames.Single().IsDisposed);
        Assert.True(factory.Sessions.Single().IsDisposed);
    }

    [Fact]
    public async Task Records_unexpected_capture_failure_and_releases_the_session()
    {
        var factory = new FakeCaptureFactory(Target("display-1", 0, 0, 1920, 1080))
        {
            AcquireException = new InvalidOperationException("显卡不可用")
        };
        var runner = new CaptureDiagnosticRunner(factory, new FakePngWriter());

        await runner.RunAsync(_outputDirectory, CancellationToken.None);

        Assert.Equal("capture-failed", await ReadFirstStatusAsync());
        Assert.True(factory.Sessions.Single().IsDisposed);
    }

    [Fact]
    public async Task Cancellation_propagates_after_releasing_frame_and_session()
    {
        using var cancellation = new CancellationTokenSource();
        var factory = new FakeCaptureFactory(Target("display-1", 0, 0, 1920, 1080));
        var writer = new FakePngWriter
        {
            OnWrite = cancellation.Cancel
        };
        var runner = new CaptureDiagnosticRunner(factory, writer);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runner.RunAsync(_outputDirectory, cancellation.Token));

        Assert.True(factory.Frames.Single().IsDisposed);
        Assert.True(factory.Sessions.Single().IsDisposed);
    }

    [Fact]
    public async Task Rejects_a_png_whose_dimensions_do_not_match_the_physical_frame()
    {
        var factory = new FakeCaptureFactory(Target("display-1", 0, 0, 1920, 1080));
        var writer = new FakePngWriter
        {
            WrittenSize = new PhysicalSize(960, 540)
        };
        var runner = new CaptureDiagnosticRunner(factory, writer);

        await runner.RunAsync(_outputDirectory, CancellationToken.None);

        Assert.Equal("invalid-png-size", await ReadFirstStatusAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }

    private async Task<string> ReadFirstStatusAsync()
    {
        using var manifest = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(_outputDirectory, "manifest.json")));
        return manifest.RootElement
            .GetProperty("outputs")[0]
            .GetProperty("status")
            .GetString()!;
    }

    private static MonitorCaptureTarget Target(
        string id,
        int adapterIndex,
        int outputIndex,
        int width,
        int height) =>
        new(
            adapterIndex,
            outputIndex,
            new MonitorDescriptor(
                id,
                $"DISPLAY{outputIndex + 1}",
                new PhysicalRect(0, 0, width, height),
                new PhysicalSize(width, height),
                96,
                96,
                outputIndex == 0));

    private sealed class FakeCaptureFactory(
        params MonitorCaptureTarget[] targets) : IMonitorCaptureFactory
    {
        public Exception? AcquireException { get; init; }

        public bool FailFirstAcquireWithAccessLost { get; init; }

        public List<FakeCaptureSession> Sessions { get; } = [];

        public List<CapturedBgraFrame> Frames { get; } = [];

        public IReadOnlyList<MonitorCaptureTarget> EnumerateOutputs() => targets;

        public IMonitorCaptureSession CreateSession(MonitorCaptureTarget target)
        {
            var exception = AcquireException;
            if (FailFirstAcquireWithAccessLost && Sessions.Count == 0)
            {
                exception = new CaptureAccessLostException();
            }

            var session = new FakeCaptureSession(target, exception, Frames);
            Sessions.Add(session);
            return session;
        }
    }

    private sealed class FakeCaptureSession(
        MonitorCaptureTarget target,
        Exception? exception,
        List<CapturedBgraFrame> frames) : IMonitorCaptureSession
    {
        public bool IsDisposed { get; private set; }

        public ValueTask<CapturedBgraFrame> AcquireFrameAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (exception is not null)
            {
                throw exception;
            }

            var frame = new CapturedBgraFrame(
                target.Monitor.FrameSize,
                checked(target.Monitor.FrameSize.Width * 4),
                new byte[
                    checked(
                        target.Monitor.FrameSize.Width
                        * target.Monitor.FrameSize.Height
                        * 4)]);
            frames.Add(frame);
            return ValueTask.FromResult(frame);
        }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class FakePngWriter : IPngFrameWriter
    {
        public Exception? Exception { get; init; }

        public Action? OnWrite { get; init; }

        public PhysicalSize? WrittenSize { get; init; }

        public List<string> WrittenFiles { get; } = [];

        public ValueTask<PhysicalSize> WriteAsync(
            string path,
            CapturedBgraFrame frame,
            CancellationToken cancellationToken)
        {
            WrittenFiles.Add(path);
            if (Exception is not null)
            {
                throw Exception;
            }

            OnWrite?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(WrittenSize ?? frame.Size);
        }
    }
}
