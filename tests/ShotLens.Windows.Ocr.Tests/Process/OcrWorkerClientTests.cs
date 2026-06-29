using System.Text.Json;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Platform.Ocr;

namespace ShotLens.Windows.Ocr.Tests.Process;

public sealed class OcrWorkerClientTests : IDisposable
{
    private readonly string _tempRoot =
        Path.Combine(Path.GetTempPath(), $"shotlens-worker-{Guid.NewGuid():N}");

    [Fact]
    public async Task Runs_worker_with_one_json_and_cleans_unique_directory()
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(OutputForRequest));
        var client = Client(factory);

        var first = await RecognizeAsync(client);
        var second = await RecognizeAsync(client);

        Assert.Equal("fixture-1", first.EngineVersion);
        Assert.Equal("fixture-1", second.EngineVersion);
        Assert.Equal(2, factory.StartInfos.Count);
        Assert.All(
            factory.StartInfos,
            startInfo =>
            {
                Assert.False(startInfo.UseShellExecute);
                Assert.True(startInfo.RedirectStandardInput);
                Assert.True(startInfo.RedirectStandardOutput);
                Assert.True(startInfo.RedirectStandardError);
                Assert.True(startInfo.CreateNoWindow);
            });
        var imagePaths = factory.Processes
            .Select(
                process =>
                {
                    using var json = JsonDocument.Parse(process.StandardInput);
                    return json.RootElement
                        .GetProperty("imagePath")
                        .GetString()!;
                })
            .ToArray();
        Assert.NotEqual(
            Path.GetDirectoryName(imagePaths[0]),
            Path.GetDirectoryName(imagePaths[1]));
        Assert.All(imagePaths, path => Assert.False(File.Exists(path)));
        Assert.Empty(Directory.GetDirectories(_tempRoot));
    }

    [Fact]
    public async Task Timeout_kills_process_tree_and_cleans_directory()
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(OutputForRequest)
            {
                NeverExits = true
            });
        var client = Client(factory, TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAsync<OcrWorkerTimeoutException>(
            () => RecognizeAsync(client));

        Assert.True(factory.Processes.Single().KilledEntireProcessTree);
        Assert.Empty(Directory.GetDirectories(_tempRoot));
    }

    [Fact]
    public async Task Crash_is_wrapped_and_cleanup_still_runs()
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(OutputForRequest)
            {
                WaitException = new IOException("process pipe closed")
            });
        var client = Client(factory);

        await Assert.ThrowsAsync<OcrWorkerProcessException>(
            () => RecognizeAsync(client));

        Assert.True(factory.Processes.Single().KilledEntireProcessTree);
        Assert.Empty(Directory.GetDirectories(_tempRoot));
    }

    [Fact]
    public async Task Start_failure_is_wrapped_and_cleanup_still_runs()
    {
        var factory = new ThrowingWorkerProcessFactory();
        var client = new OcrWorkerClient(
            factory,
            "missing-worker.exe",
            _tempRoot,
            TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<OcrWorkerProcessException>(
            () => RecognizeAsync(client));

        Assert.Empty(Directory.GetDirectories(_tempRoot));
    }

    [Fact]
    public async Task Rejects_non_zero_exit_code()
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(OutputForRequest)
            {
                ExitCode = 9
            });

        var exception = await Assert.ThrowsAsync<OcrWorkerExitException>(
            () => RecognizeAsync(Client(factory)));

        Assert.Equal(9, exception.ExitCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \r\n")]
    public async Task Rejects_empty_stdout(string stdout)
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(_ => stdout));

        await Assert.ThrowsAsync<OcrWorkerOutputException>(
            () => RecognizeAsync(Client(factory)));
    }

    [Fact]
    public async Task Rejects_extra_stdout()
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(
                input => $"{OutputForRequest(input)}\nunexpected"));

        await Assert.ThrowsAsync<OcrProtocolException>(
            () => RecognizeAsync(Client(factory)));
    }

    [Fact]
    public async Task Rejects_stdout_larger_than_sixteen_mebibytes()
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(
                _ => new string(
                    'x',
                    OcrWorkerClient.MaxStandardOutputBytes + 1)));

        await Assert.ThrowsAsync<OcrWorkerOutputException>(
            () => RecognizeAsync(Client(factory)));
    }

    [Fact]
    public async Task Rejects_mismatched_request_id()
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(
                input =>
                {
                    var request = OcrProtocolJson.DeserializeRequest(input);
                    return OcrProtocolJson.SerializeResponse(
                        Response(Guid.NewGuid().ToString("N")),
                        request.ImageSize);
                }));

        await Assert.ThrowsAsync<OcrWorkerOutputException>(
            () => RecognizeAsync(Client(factory)));
    }

    [Fact]
    public async Task Cancellation_kills_process_tree_and_cleans_directory()
    {
        var factory = new FakeWorkerProcessFactory(
            new FakeWorkerBehavior(OutputForRequest)
            {
                NeverExits = true
            });
        var client = Client(factory, TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource(
            TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => RecognizeAsync(client, cancellation.Token));

        Assert.True(factory.Processes.Single().KilledEntireProcessTree);
        Assert.Empty(Directory.GetDirectories(_tempRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private OcrWorkerClient Client(
        FakeWorkerProcessFactory factory,
        TimeSpan? timeout = null) =>
        new(
            factory,
            "fixture-worker.exe",
            _tempRoot,
            timeout ?? TimeSpan.FromSeconds(1));

    private static Task<OcrProtocolResponse> RecognizeAsync(
        OcrWorkerClient client,
        CancellationToken cancellationToken = default) =>
        client.RecognizeAsync(
            OcrEngineIds.Fixture,
            new byte[] { 137, 80, 78, 71 },
            new PhysicalSize(800, 600),
            ["en"],
            cancellationToken);

    private static string OutputForRequest(string input)
    {
        var request = OcrProtocolJson.DeserializeRequest(input);
        return OcrProtocolJson.SerializeResponse(
            Response(request.RequestId),
            request.ImageSize);
    }

    private static OcrProtocolResponse Response(string requestId) =>
        new(
            OcrProtocol.CurrentVersion,
            requestId,
            OcrEngineIds.Fixture,
            "fixture-1",
            1,
            []);

    private sealed record FakeWorkerBehavior(
        Func<string, string> OutputFactory)
    {
        public bool NeverExits { get; init; }

        public int ExitCode { get; init; }

        public Exception? WaitException { get; init; }
    }

    private sealed class FakeWorkerProcessFactory(
        params FakeWorkerBehavior[] behaviors) : IOcrWorkerProcessFactory
    {
        private readonly Queue<FakeWorkerBehavior> _behaviors = new(behaviors);

        public List<OcrWorkerStartInfo> StartInfos { get; } = [];

        public List<FakeWorkerProcess> Processes { get; } = [];

        public IOcrWorkerProcess Start(OcrWorkerStartInfo startInfo)
        {
            StartInfos.Add(startInfo);
            var behavior = _behaviors.Count > 1
                ? _behaviors.Dequeue()
                : _behaviors.Peek();
            var process = new FakeWorkerProcess(behavior);
            Processes.Add(process);
            return process;
        }
    }

    private sealed class FakeWorkerProcess(
        FakeWorkerBehavior behavior) : IOcrWorkerProcess
    {
        public string StandardInput { get; private set; } = "";

        public bool KilledEntireProcessTree { get; private set; }

        public bool HasExited { get; private set; }

        public int ExitCode => behavior.ExitCode;

        public Task WriteStandardInputAsync(
            string value,
            CancellationToken cancellationToken)
        {
            StandardInput = value;
            return Task.CompletedTask;
        }

        public Task<string> ReadStandardOutputAsync(
            int maxUtf8Bytes,
            CancellationToken cancellationToken) =>
            Task.FromResult(behavior.OutputFactory(StandardInput));

        public Task<string> ReadStandardErrorAsync(
            int maxUtf8Bytes,
            CancellationToken cancellationToken) =>
            Task.FromResult("fixture diagnostic");

        public async Task WaitForExitAsync(CancellationToken cancellationToken)
        {
            if (behavior.WaitException is not null)
            {
                throw behavior.WaitException;
            }

            if (behavior.NeverExits)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return;
            }

            HasExited = true;
        }

        public void Kill(bool entireProcessTree)
        {
            KilledEntireProcessTree = entireProcessTree;
            HasExited = true;
        }

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingWorkerProcessFactory : IOcrWorkerProcessFactory
    {
        public IOcrWorkerProcess Start(OcrWorkerStartInfo startInfo) =>
            throw new FileNotFoundException("worker missing");
    }
}
