namespace ShotLens.Windows.Platform.Ocr;

public sealed record OcrWorkerStartInfo(
    string FileName,
    bool UseShellExecute,
    bool RedirectStandardInput,
    bool RedirectStandardOutput,
    bool RedirectStandardError,
    bool CreateNoWindow);
