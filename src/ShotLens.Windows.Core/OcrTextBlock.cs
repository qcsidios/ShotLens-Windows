namespace ShotLens.Windows.Core;

public sealed record OcrTextBlock(
    string Text,
    double X,
    double Y,
    double Width,
    double Height,
    string DetectedLanguage);
