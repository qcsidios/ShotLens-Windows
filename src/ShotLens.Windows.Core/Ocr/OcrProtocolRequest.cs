using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Core.Ocr;

public sealed record OcrProtocolRequest(
    int ProtocolVersion,
    string RequestId,
    string Engine,
    string ImagePath,
    PhysicalSize ImageSize,
    string[] LanguageHints);
