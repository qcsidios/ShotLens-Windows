namespace ShotLens.Windows.Core.Translation;

public sealed record TranslationResult(string[] Translations);

public sealed class TranslationException : Exception
{
    public TranslationException(string message)
        : base(message)
    {
    }

    public TranslationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
