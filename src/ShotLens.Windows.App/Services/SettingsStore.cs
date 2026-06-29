using System.IO;
using System.Text.Json;

namespace ShotLens.Windows.App.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string settingsPath;

    public SettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "ShotLens");
        Directory.CreateDirectory(directory);
        settingsPath = Path.Combine(directory, "settings.json");
    }

    public ShotLensSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return ShotLensSettings.Default;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<ShotLensSettings>(json) ?? ShotLensSettings.Default;
        }
        catch
        {
            return ShotLensSettings.Default;
        }
    }

    public void Save(ShotLensSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(settingsPath, json);
    }
}
