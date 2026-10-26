using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void CreateDefault_UsesExpectedValues()
    {
        var settings = AppSettings.CreateDefault();
        Assert.Equal("Ctrl+Alt+Shift+R", settings.Hotkey);
        Assert.Equal(3, settings.CountdownSeconds);
        Assert.Equal(OutputFormat.Mp4, settings.DefaultFormat);
        Assert.Equal(AudioMode.Off, settings.AudioMode);
        Assert.False(settings.StartWithWindows);
        Assert.False(settings.HasShownFirstRunTip);
        Assert.EndsWith(Path.Combine("Videos", "ViewportCap"), settings.OutputFolder);
    }

    [Fact]
    public void Normalize_RepairsInvalidValues()
    {
        var settings = new AppSettings { Hotkey = "not a hotkey", CountdownSeconds = 99, DefaultFormat = (OutputFormat)99, AudioMode = (AudioMode)99, OutputFolder = " " };
        settings.Normalize();
        Assert.Equal("Ctrl+Alt+Shift+R", settings.Hotkey);
        Assert.Equal(10, settings.CountdownSeconds);
        Assert.Equal(OutputFormat.Mp4, settings.DefaultFormat);
        Assert.Equal(AudioMode.Off, settings.AudioMode);
        Assert.False(string.IsNullOrWhiteSpace(settings.OutputFolder));
    }
}
