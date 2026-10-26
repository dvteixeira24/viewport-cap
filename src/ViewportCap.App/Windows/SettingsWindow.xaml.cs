using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ScreenRecorderLib;
using ViewportCap.App.Services;
using ViewportCap.Core;
using MessageBox = System.Windows.MessageBox;

namespace ViewportCap.App.Windows;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ISettingsStore _store;
    private readonly Func<HotkeyGesture, bool> _hotkeyTester;
    private readonly Action<AppSettings> _saved;
    private readonly List<AudioDeviceOption> _microphones = new();
    private readonly List<AudioDeviceOption> _systemAudio = new();

    public SettingsWindow(AppSettings settings, ISettingsStore store, Func<HotkeyGesture, bool> hotkeyTester, Action<AppSettings> onSaved)
    {
        InitializeComponent();
        _settings = settings;
        _store = store;
        _hotkeyTester = hotkeyTester;
        _saved = onSaved;
        HotkeyBox.Text = settings.Hotkey;
        OutputFolderBox.Text = settings.OutputFolder;
        CountdownBox.ItemsSource = new[] { 0, 3, 5, 10 };
        CountdownBox.SelectedItem = settings.CountdownSeconds;
        FormatBox.ItemsSource = Enum.GetValues<OutputFormat>();
        FormatBox.SelectedItem = settings.DefaultFormat;
        AudioModeBox.ItemsSource = Enum.GetValues<AudioMode>();
        AudioModeBox.SelectedItem = settings.AudioMode;
        StartWithWindowsBox.IsChecked = settings.StartWithWindows;
        VersionText.Text = $"Version {typeof(SettingsWindow).Assembly.GetName().Version?.ToString(3) ?? "0.1.0"}";
        FillAudioDevices(settings);
    }

    private void FillAudioDevices(AppSettings settings)
    {
        AddDefault(_microphones, settings.MicrophoneDeviceName);
        AddDefault(_systemAudio, settings.SystemAudioDeviceName);
        try
        {
            foreach (var device in Recorder.GetSystemAudioCaptureDevices())
                _microphones.Add(new AudioDeviceOption(device.FriendlyName, device.DeviceName));
        }
        catch
        {
            // Keep the default option if device enumeration is unavailable.
        }

        try
        {
            foreach (var device in Recorder.GetSystemAudioLoopbackDevices())
                _systemAudio.Add(new AudioDeviceOption(device.FriendlyName, device.DeviceName));
        }
        catch
        {
            // Keep the default option if device enumeration is unavailable.
        }

        MicrophoneBox.ItemsSource = _microphones;
        MicrophoneBox.DisplayMemberPath = nameof(AudioDeviceOption.Name);
        MicrophoneBox.SelectedItem = FindDevice(_microphones, settings.MicrophoneDeviceName);
        SystemAudioBox.ItemsSource = _systemAudio;
        SystemAudioBox.DisplayMemberPath = nameof(AudioDeviceOption.Name);
        SystemAudioBox.SelectedItem = FindDevice(_systemAudio, settings.SystemAudioDeviceName);
    }

    private static void AddDefault(List<AudioDeviceOption> devices, string? selectedDevice)
    {
        devices.Add(new AudioDeviceOption("Default device", null));
        if (!string.IsNullOrWhiteSpace(selectedDevice))
            devices.Add(new AudioDeviceOption($"Saved: {selectedDevice}", selectedDevice));
    }

    private static AudioDeviceOption FindDevice(List<AudioDeviceOption> devices, string? deviceName)
    {
        return devices.FirstOrDefault(device => string.Equals(device.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
            ?? devices[0];
    }

    private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var modifiers = HotkeyModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= HotkeyModifiers.Control;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= HotkeyModifiers.Alt;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= HotkeyModifiers.Shift;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) modifiers |= HotkeyModifiers.Win;
        var gesture = new HotkeyGesture(modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
        if (HotkeyGesture.TryParse(gesture.ToString(), out _))
            HotkeyBox.Text = gesture.ToString();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = OutputFolderBox.Text };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            OutputFolderBox.Text = dialog.SelectedPath;
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(AppPaths.LogsDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = AppPaths.LogsDirectory,
            UseShellExecute = true
        });
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!HotkeyGesture.TryParse(HotkeyBox.Text, out var hotkey) || !_hotkeyTester(hotkey) || string.IsNullOrWhiteSpace(OutputFolderBox.Text))
        {
            MessageBox.Show("Enter a valid, available hotkey and output folder.", "viewport-cap");
            return;
        }

        var updated = new AppSettings
        {
            Hotkey = hotkey.ToString(),
            OutputFolder = OutputFolderBox.Text.Trim(),
            CountdownSeconds = CountdownBox.SelectedItem is int countdown ? countdown : 3,
            DefaultFormat = FormatBox.SelectedItem is OutputFormat format ? format : OutputFormat.Mp4,
            AudioMode = AudioModeBox.SelectedItem is AudioMode audio ? audio : AudioMode.Off,
            MicrophoneDeviceName = (MicrophoneBox.SelectedItem as AudioDeviceOption)?.DeviceName,
            SystemAudioDeviceName = (SystemAudioBox.SelectedItem as AudioDeviceOption)?.DeviceName,
            StartWithWindows = StartWithWindowsBox.IsChecked == true,
            HasShownFirstRunTip = _settings.HasShownFirstRunTip
        };
        updated.Normalize();
        try
        {
            Directory.CreateDirectory(updated.OutputFolder);
            _store.Save(updated);
        }
        catch (Exception ex)
        {
            FileLog.Error("Could not save settings.", ex);
            MessageBox.Show(ex.Message, "viewport-cap");
            return;
        }

        _saved(updated);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private sealed record AudioDeviceOption(string Name, string? DeviceName);
}
