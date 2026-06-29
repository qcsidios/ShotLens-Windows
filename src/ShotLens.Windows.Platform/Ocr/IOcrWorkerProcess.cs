namespace ShotLens.Windows.Platform.Ocr;

public interface IOcrWorkerProcess : IDisposable
{
    bool HasExited { get; }

    int ExitCode { get; }

    Task WriteStandardInputAsync(
        string value,
        CancellationToken cancellationToken);

    Task<string> ReadStandardOutputAsync(
        int maxUtf8Bytes,
        CancellationToken cancellationToken);

    Task<string> ReadStandardErrorAsync(
        int maxUtf8Bytes,
        CancellationToken cancellationToken);

    Task WaitForExitAsync(CancellationToken cancellationToken);

    void Kill(bool entireProcessTree);
}

public interface IOcrWorkerProcessFactory
{
    IOcrWorkerProcess Start(OcrWorkerStartInfo startInfo);
}
