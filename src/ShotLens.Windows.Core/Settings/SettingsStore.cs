using System.Text.Json;

namespace ShotLens.Windows.Core.Settings;

public sealed class SettingsStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public AppSettings Load()
    {
        if (!File.Exists(path))
        {
            return AppSettings.Default;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(path),
                JsonOptions);
            return settings ?? AppSettings.Default;
        }
        catch (JsonException)
        {
            BackupInvalidFile();
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(
            temporaryPath,
            JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private void BackupInvalidFile()
    {
        var backupPath = Path.Combine(
            Path.GetDirectoryName(path)!,
            $"{Path.GetFileName(path)}.invalid-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}");
        File.Move(path, backupPath, overwrite: true);
    }
}
