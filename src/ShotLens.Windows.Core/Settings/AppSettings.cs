using ShotLens.Windows.Core.Translation;

namespace ShotLens.Windows.Core.Settings;

public sealed record AppSettings(
    ApiConfiguration Api,
    string TargetLanguage)
{
    public static AppSettings Default { get; } = new(
        ApiConfiguration.Default,
        "中文");
}
