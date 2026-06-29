using System.Text;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;

namespace ShotLens.Windows.Platform.Ocr;

public sealed class OcrWorkerClient
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public const int MaxStandardOutputBytes = 16 * 1024 * 1024;

    public const int MaxStandardErrorBytes = 1024 * 1024;

    private readonly IOcrWorkerProcessFactory _processFactory;
    private readonly string _workerPath;
    private readonly string _tempRoot;
    private readonly TimeSpan _timeout;

    public OcrWorkerClient(
        IOcrWorkerProcessFactory processFactory,
        string workerPath,
        string tempRoot,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(workerPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempRoot);
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        _processFactory = processFactory;
        _workerPath = workerPath;
        _tempRoot = tempRoot;
        _timeout = timeout ?? DefaultTimeout;
    }

    public async Task<OcrProtocolResponse> RecognizeAsync(
        string engine,
        ReadOnlyMemory<byte> pngBytes,
        PhysicalSize imageSize,
        string[] languageHints,
        CancellationToken cancellationToken)
    {
        if (pngBytes.IsEmpty)
        {
            throw new ArgumentException("OCR 图片不能为空。", nameof(pngBytes));
        }

        Directory.CreateDirectory(_tempRoot);
        var requestId = Guid.NewGuid().ToString("N");
        var requestDirectory = Path.Combine(_tempRoot, $"request-{requestId}");
        Directory.CreateDirectory(requestDirectory);
        IOcrWorkerProcess? process = null;
        try
        {
            var imagePath = Path.Combine(requestDirectory, "input.png");
            await using (var imageStream = new FileStream(
                imagePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await imageStream.WriteAsync(pngBytes, cancellationToken);
            }
            var request = new OcrProtocolRequest(
                OcrProtocol.CurrentVersion,
                requestId,
                engine,
                imagePath,
                imageSize,
                languageHints);
            var requestJson = OcrProtocolJson.SerializeRequest(request);

            try
            {
                process = _processFactory.Start(
                    new OcrWorkerStartInfo(
                        _workerPath,
                        UseShellExecute: false,
                        RedirectStandardInput: true,
                        RedirectStandardOutput: true,
                        RedirectStandardError: true,
                        CreateNoWindow: true));
            }
            catch (OcrWorkerProcessException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new OcrWorkerProcessException(
                    "无法启动 OCR Worker。",
                    exception);
            }

            await process.WriteStandardInputAsync(
                requestJson,
                cancellationToken);

            using var timeoutCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellation.CancelAfter(_timeout);
            var workerToken = timeoutCancellation.Token;
            var stdoutTask = process.ReadStandardOutputAsync(
                MaxStandardOutputBytes,
                workerToken);
            var stderrTask = process.ReadStandardErrorAsync(
                MaxStandardErrorBytes,
                workerToken);
            try
            {
                await process.WaitForExitAsync(workerToken);
                var stdout = await stdoutTask;
                _ = await stderrTask;

                if (process.ExitCode != 0)
                {
                    throw new OcrWorkerExitException(process.ExitCode);
                }

                if (string.IsNullOrWhiteSpace(stdout))
                {
                    throw new OcrWorkerOutputException(
                        "OCR Worker stdout 为空。");
                }

                if (Encoding.UTF8.GetByteCount(stdout)
                    > MaxStandardOutputBytes)
                {
                    throw new OcrWorkerOutputException(
                        "OCR Worker stdout 超过 16 MiB。");
                }

                var response = OcrProtocolJson.DeserializeResponse(
                    stdout,
                    imageSize);
                if (!string.Equals(
                        response.RequestId,
                        requestId,
                        StringComparison.Ordinal)
                    || !string.Equals(
                        response.Engine,
                        engine,
                        StringComparison.Ordinal))
                {
                    throw new OcrWorkerOutputException(
                        "OCR Worker 响应与请求不匹配。");
                }

                return response;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                Kill(process);
                throw new OcrWorkerTimeoutException();
            }
            catch (OperationCanceledException)
            {
                Kill(process);
                throw;
            }
            catch (OcrWorkerProcessException)
            {
                Kill(process);
                throw;
            }
            catch (OcrProtocolException)
            {
                Kill(process);
                throw;
            }
            catch (Exception exception)
            {
                Kill(process);
                throw new OcrWorkerProcessException(
                    "OCR Worker 进程通信失败。",
                    exception);
            }
        }
        finally
        {
            process?.Dispose();
            if (Directory.Exists(requestDirectory))
            {
                Directory.Delete(requestDirectory, recursive: true);
            }
        }
    }

    private static void Kill(IOcrWorkerProcess process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
}
