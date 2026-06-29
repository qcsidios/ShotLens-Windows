namespace ShotLens.Windows.Core.Translation;

public enum ApiCredentialMode
{
    DefaultFree,
    Custom,
    Disabled
}

public sealed record ApiConfiguration(
    ApiCredentialMode Mode,
    string BaseUrl,
    string Model,
    string? UserApiKey)
{
    public static ApiConfiguration Default { get; } = new(
        ApiCredentialMode.DefaultFree,
        "https://api.siliconflow.cn/v1",
        "tencent/Hunyuan-MT-7B",
        null);
}
