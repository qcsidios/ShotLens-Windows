namespace ShotLens.Windows.Core.Ocr.Benchmark;

public sealed class OcrBenchmarkValidationException : Exception
{
    public OcrBenchmarkValidationException(string message)
        : base(message)
    {
    }
}
