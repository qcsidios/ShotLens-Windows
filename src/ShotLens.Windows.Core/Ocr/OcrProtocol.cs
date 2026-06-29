namespace ShotLens.Windows.Core.Ocr;

public static class OcrProtocol
{
    public const int CurrentVersion = 1;
}

public static class OcrEngineIds
{
    public const string Fixture = "fixture";

    public const string OnnxPaddleOcr = "onnx-paddleocr";

    public const string PaddleSharp = "paddlesharp";

    public static bool IsKnown(string value) =>
        value is Fixture or OnnxPaddleOcr or PaddleSharp;
}
