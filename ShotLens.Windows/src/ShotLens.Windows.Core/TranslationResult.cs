namespace ShotLens.Windows.Core;

public sealed record TranslationResult(
    OcrTextBlock SourceBlock,
    string Translation);
