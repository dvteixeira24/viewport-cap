namespace ViewportCap.Core;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public string Hotkey { get; set; } = "Ctrl+Alt+Shift+R";
    public string OutputFolder { get; set; } = string.Empty;
    public int CountdownSeconds { get; set; } = 3;
    public OutputFormat DefaultFormat { get; set; } = OutputFormat.Mp4;
    public AudioMode AudioMode { get; set; } = AudioMode.Off;
    public string? MicrophoneDeviceName { get; set; }
    public string? SystemAudioDeviceName { get; set; }
    public bool StartWithWindows { get; set; }
    public bool HasShownFirstRunTip { get; set; }
    public static AppSettings CreateDefault() => new() { OutputFolder = DefaultOutputFolder() };
    public static string DefaultOutputFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ViewportCap");
    public void Normalize()
    {
        SchemaVersion = 1;
        if (string.IsNullOrWhiteSpace(OutputFolder)) OutputFolder = DefaultOutputFolder();
        CountdownSeconds = Math.Clamp(CountdownSeconds, 0, 10);
        if (!HotkeyGesture.TryParse(Hotkey, out _)) Hotkey = HotkeyGesture.Default.ToString();
        if (!Enum.IsDefined(DefaultFormat)) DefaultFormat = OutputFormat.Mp4;
        if (!Enum.IsDefined(AudioMode)) AudioMode = AudioMode.Off;
    }
}
