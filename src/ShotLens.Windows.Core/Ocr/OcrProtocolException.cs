namespace ShotLens.Windows.Core.Ocr;

public sealed class OcrProtocolException : Exception
{
    public OcrProtocolException(string message)
        : base(message)
    {
    }

    public OcrProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
