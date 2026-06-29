using ShotLens.Windows.Core.Settings;
using ShotLens.Windows.Core.Translation;

namespace ShotLens.Windows.Core.Tests.Settings;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"shotlens-settings-{Guid.NewGuid():N}");

    public SettingsStoreTests() => Directory.CreateDirectory(directory);

    [Fact]
    public void Default_settings_use_free_api_without_storing_default_key()
    {
        var settings = AppSettings.Default;

        Assert.Equal(ApiCredentialMode.DefaultFree, settings.Api.Mode);
        Assert.Equal("https://api.siliconflow.cn/v1", settings.Api.BaseUrl);
        Assert.Equal("tencent/Hunyuan-MT-7B", settings.Api.Model);
        Assert.Null(settings.Api.UserApiKey);
    }

    [Fact]
    public void Custom_api_key_is_saved_plainly_in_local_settings_file()
    {
        var path = Path.Combine(directory, "settings.json");
        var store = new SettingsStore(path);
        var settings = AppSettings.Default with
        {
            Api = AppSettings.Default.Api with
            {
                Mode = ApiCredentialMode.Custom,
                UserApiKey = "sk-user-local"
            }
        };

        store.Save(settings);

        var json = File.ReadAllText(path);
        Assert.Contains("sk-user-local", json);
        Assert.Equal(settings, store.Load());
    }

    [Fact]
    public void Corrupt_settings_are_backed_up_and_default_settings_are_returned()
    {
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ broken");
        var store = new SettingsStore(path);

        var settings = store.Load();

        Assert.Equal(AppSettings.Default, settings);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(directory, "settings.json.invalid-*"));
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
