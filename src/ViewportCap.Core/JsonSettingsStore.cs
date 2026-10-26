using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViewportCap.Core;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public JsonSettingsStore(string path) => _path = path;
    public AppSettings Load()
    {
        if (!File.Exists(_path)) return AppSettings.CreateDefault();
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), JsonOptions);
            if (settings is null) return AppSettings.CreateDefault();
            settings.Normalize(); return settings;
        }
        catch { TryBackupCorruptFile(); return AppSettings.CreateDefault(); }
    }
    public void Save(AppSettings settings)
    {
        settings.Normalize();
        var directory = Path.GetDirectoryName(_path);
        if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Settings path has no directory.");
        Directory.CreateDirectory(directory); var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions), new UTF8Encoding(false));
        File.Move(temp, _path, true);
    }
    private void TryBackupCorruptFile()
    {
        try { File.Copy(_path, $"{_path}.bad-{DateTime.Now:yyyyMMdd-HHmmss}", true); }
        catch { /* A failed backup must not prevent the app from starting. */ }
    }
}
