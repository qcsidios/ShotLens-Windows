using ShotLens.Windows.Core;

namespace ShotLens.Windows.App.Services;

public sealed class ShotLensSettings
{
    public string ApiEndpoint { get; init; } = TranslationSettings.DefaultApiEndpoint;
    public string ApiKey { get; init; } = "";
    public string Model { get; init; } = TranslationSettings.DefaultModel;
    public bool DefaultFallbackEnabled { get; init; } = true;
    public ShortcutGesture Shortcut { get; init; } = ShortcutGesture.Default;

    public static ShotLensSettings Default => new();

    public TranslationSettings ToTranslationSettings() =>
        new(ApiEndpoint, ApiKey, Model, DefaultFallbackEnabled);
}
