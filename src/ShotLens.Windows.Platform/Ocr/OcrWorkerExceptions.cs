namespace ShotLens.Windows.Platform.Ocr;

public class OcrWorkerProcessException : Exception
{
    public OcrWorkerProcessException(string message)
        : base(message)
    {
    }

    public OcrWorkerProcessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class OcrWorkerTimeoutException : OcrWorkerProcessException
{
    public OcrWorkerTimeoutException()
        : base("OCR Worker 处理超时。")
    {
    }
}

public sealed class OcrWorkerExitException : OcrWorkerProcessException
{
    public OcrWorkerExitException(int exitCode)
        : base($"OCR Worker 异常退出，退出码 {exitCode}。") =>
        ExitCode = exitCode;

    public int ExitCode { get; }
}

public sealed class OcrWorkerOutputException : OcrWorkerProcessException
{
    public OcrWorkerOutputException(string message)
        : base(message)
    {
    }
}
