namespace ShotLens.Windows.Core;

public sealed record TranslationSettings(
    string ApiEndpoint,
    string ApiKey,
    string Model,
    bool DefaultFallbackEnabled = true)
{
    public const string DefaultApiEndpoint = "https://api.siliconflow.cn/v1";
    public const string DefaultApiKey = "sk-iiwyxcrwfaiqixpbfitsogijhfjsiolqtntqszuixgohjpnb";
    public const string DefaultModel = "tencent/Hunyuan-MT-7B";
    public const string LimitedFreeModelNotice = "混元模型当前限免，后续以服务商政策为准。";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(EffectiveApiEndpoint)
        && !string.IsNullOrWhiteSpace(EffectiveApiKey);

    public bool UsesDefaultApiKey => ShouldUseDefaultFallback;

    public string EffectiveApiEndpoint
    {
        get
        {
            var trimmed = ApiEndpoint.Trim();
            return ShouldUseDefaultFallback && trimmed.Length == 0 ? DefaultApiEndpoint : trimmed;
        }
    }

    public string EffectiveApiKey
    {
        get
        {
            var trimmed = ApiKey.Trim();
            return ShouldUseDefaultFallback ? DefaultApiKey : trimmed;
        }
    }

    public string EffectiveModel
    {
        get
        {
            var trimmed = Model.Trim();
            return ShouldUseDefaultFallback && trimmed.Length == 0 ? DefaultModel : trimmed;
        }
    }

    public Uri? ChatCompletionsUri
    {
        get
        {
            var endpoint = NormalizedEndpoint;
            if (endpoint.Length == 0)
            {
                return null;
            }

            if (HasSuffixPath(endpoint, "/chat/completions"))
            {
                return new Uri(endpoint);
            }

            if (HasSuffixPath(endpoint, "/models"))
            {
                return new Uri(DropSuffix(endpoint, "/models") + "/chat/completions");
            }

            return new Uri(endpoint + "/chat/completions");
        }
    }

    public static TranslationSettings Default => new("", "", "");

    public static TranslationSettings Cleared => new("", "", "", DefaultFallbackEnabled: false);

    private bool ShouldUseDefaultFallback
    {
        get
        {
            if (!DefaultFallbackEnabled || ApiKey.Trim().Length != 0)
            {
                return false;
            }

            var endpoint = ApiEndpoint.Trim();
            var model = Model.Trim();
            return (endpoint.Length == 0 || endpoint == DefaultApiEndpoint)
                && (model.Length == 0 || model == DefaultModel);
        }
    }

    private string NormalizedEndpoint => EffectiveApiEndpoint.Trim().TrimEnd('/');

    private static bool HasSuffixPath(string endpoint, string suffix) =>
        endpoint.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

    private static string DropSuffix(string endpoint, string suffix) =>
        HasSuffixPath(endpoint, suffix) ? endpoint[..^suffix.Length] : endpoint;
}
