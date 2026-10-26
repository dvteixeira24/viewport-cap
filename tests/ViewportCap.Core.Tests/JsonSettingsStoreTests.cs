using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "viewport-cap-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing() => Assert.Equal("Ctrl+Alt+Shift+R", new JsonSettingsStore(Path.Combine(_dir, "settings.json")).Load().Hotkey);

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var store = new JsonSettingsStore(Path.Combine(_dir, "settings.json"));
        var expected = AppSettings.CreateDefault();
        expected.Hotkey = "Ctrl+Shift+F8"; expected.CountdownSeconds = 5; expected.DefaultFormat = OutputFormat.Gif; expected.AudioMode = AudioMode.Both; expected.HasShownFirstRunTip = true;
        store.Save(expected);
        var actual = store.Load();
        Assert.Equal(expected.Hotkey, actual.Hotkey); Assert.Equal(expected.CountdownSeconds, actual.CountdownSeconds);
        Assert.Equal(expected.DefaultFormat, actual.DefaultFormat); Assert.Equal(expected.AudioMode, actual.AudioMode); Assert.True(actual.HasShownFirstRunTip);
    }

    [Fact]
    public void Load_BacksUpCorruptFile_AndReturnsDefaults()
    {
        var path = Path.Combine(_dir, "settings.json"); Directory.CreateDirectory(_dir); File.WriteAllText(path, "{ broken json");
        var settings = new JsonSettingsStore(path).Load();
        Assert.Equal("Ctrl+Alt+Shift+R", settings.Hotkey); Assert.Single(Directory.GetFiles(_dir, "settings.json.bad-*"));
    }
}
