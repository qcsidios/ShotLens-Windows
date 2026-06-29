using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Core.Ocr;

public sealed record OcrProtocolResponse(
    int ProtocolVersion,
    string RequestId,
    string Engine,
    string EngineVersion,
    long ElapsedMilliseconds,
    OcrTextBlock[] Blocks);

public sealed record OcrTextBlock(
    string Text,
    PhysicalRect Bounds,
    double Confidence,
    string Language,
    double FontSizePixels,
    double Brightness,
    int Order);
