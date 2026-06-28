using ShotLens.Windows.Core;

namespace ShotLens.Windows.App.Services;

public sealed record ShotLensSettings(
    string ApiEndpoint,
    string ApiKey,
    string Model,
    bool DefaultFallbackEnabled)
{
    public static ShotLensSettings Default => new(
        TranslationSettings.DefaultApiEndpoint,
        "",
        TranslationSettings.DefaultModel,
        true);

    public TranslationSettings ToTranslationSettings() =>
        new(ApiEndpoint, ApiKey, Model, DefaultFallbackEnabled);
}
