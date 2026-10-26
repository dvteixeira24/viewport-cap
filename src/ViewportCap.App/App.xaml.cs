using System.Diagnostics;
using System.IO;
using System.Windows;
using ViewportCap.App.Services;
using ViewportCap.App.Windows;
using ViewportCap.Capture;
using ViewportCap.Core;
using ViewportCap.Encoding;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ViewportCap.App;

public partial class App : Application
{
    private SingleInstanceService? _single;
    private HotkeyService? _hotkey;
    private TrayService? _tray;
    private StartupService? _startup;
    private ISettingsStore? _store;
    private AppSettings? _settings;
    private SettingsWindow? _window;
    private ScreenRecorderCaptureEngine? _capture;
    private CaptureFlowController? _flow;
    private ToastService? _toast;
    private Task<IExporter?>? _exporterInit;
    private volatile bool _captureAvailable;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            FileLog.Error("Unhandled UI exception.", args.Exception);
            MessageBox.Show(
                $"viewport-cap hit an error:\n\n{args.Exception.Message}",
                "viewport-cap",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppPaths.EnsureCreated();
        AppPaths.CleanupOldRecordings(TimeSpan.FromDays(7));
        FileLog.Info("viewport-cap starting.");

        _single = new SingleInstanceService();
        if (!_single.TryAcquire())
        {
            Shutdown();
            return;
        }

        _single.Listen(() => Dispatcher.Invoke(ShowSettings));

        _store = new JsonSettingsStore(AppPaths.SettingsFile);
        _settings = _store.Load();
        _toast = new ToastService();

        _startup = new StartupService();
        try
        {
            _startup.SetEnabled(_settings.StartWithWindows);
        }
        catch (Exception ex)
        {
            FileLog.Error("Could not update Start with Windows.", ex);
        }

        _hotkey = new HotkeyService();
        if (HotkeyGesture.TryParse(_settings.Hotkey, out var gesture) &&
            !_hotkey.TryRegister(gesture, OnCapture))
        {
            var message = $"The hotkey {gesture} is already in use. Choose another hotkey in Settings.";
            FileLog.Warn(message);
            MessageBox.Show(message, "viewport-cap", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _capture = new ScreenRecorderCaptureEngine();
        _captureAvailable = true;
        _flow = new CaptureFlowController(
            () => _settings!,
            _store,
            _capture,
            () => _exporterInit ?? Task.FromResult<IExporter?>(null),
            _toast.Show,
            () => _captureAvailable);

        _tray = new TrayService(_settings.Hotkey, _settings.StartWithWindows);
        _tray.CaptureRequested += OnCapture;
        _tray.SettingsRequested += ShowSettings;
        _tray.OpenRecordingsRequested += OpenFolder;
        _tray.StartWithWindowsChanged += enabled =>
        {
            _settings.StartWithWindows = enabled;
            _store.Save(_settings);
            try
            {
                _startup.SetEnabled(enabled);
            }
            catch (Exception ex)
            {
                FileLog.Error("Could not update Start with Windows.", ex);
            }
        };
        _tray.ExitRequested += () =>
        {
            if (_flow is { IsIdle: false })
            {
                if (MessageBox.Show(
                        "A capture is in progress. Exit anyway?",
                        "viewport-cap",
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _flow.Cancel();
                }
                else
                {
                    return;
                }
            }

            Shutdown();
        };

        if (!_settings.HasShownFirstRunTip)
        {
            _tray.ShowBalloon("viewport-cap", $"Press {_settings.Hotkey} to capture a region.");
            _settings.HasShownFirstRunTip = true;
            _store.Save(_settings);
        }

        StartBackgroundInitialization();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _flow?.Cancel();
        _tray?.Dispose();
        _hotkey?.Dispose();
        _single?.Dispose();
        base.OnExit(e);
    }

    private void StartBackgroundInitialization()
    {
        _exporterInit = Task.Run(CreateExporter);

        _ = Task.Run(() =>
        {
            try
            {
                CapturePreflight.EnsureNativeLibraryLoads();
                try
                {
                    Dispatcher.Invoke(() => _captureAvailable = true);
                }
                catch
                {
                    // The app may be shutting down.
                }
            }
            catch (Exception ex)
            {
                _captureAvailable = false;
                FileLog.Error("Screen capture prerequisites are missing.", ex);
                try
                {
                    Dispatcher.Invoke(() => MessageBox.Show(
                        "Screen capture is unavailable. Install the Microsoft Visual C++ runtime and Media Feature Pack if required.",
                        "viewport-cap",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
                }
                catch
                {
                    // The app may be shutting down.
                }
            }
        });
    }

    private static IExporter? CreateExporter()
    {
        var path = FfmpegLocator.Find();
        if (path is null)
        {
            FileLog.Warn("ffmpeg.exe was not found. GIF/WebM export is disabled.");
            return null;
        }

        try
        {
            var runner = new SystemProcessRunner();
            // No timeout: the probe runs off the UI thread and antivirus can stall ffmpeg.exe for minutes.
            var caps = FfmpegProbe
                .ProbeAsync(path, runner, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            return new FfmpegExporter(path, caps, runner);
        }
        catch (Exception ex)
        {
            FileLog.Error("Could not initialize FFmpeg.", ex);
            return null;
        }
    }

    private void OnCapture()
    {
        _flow?.HandleHotkey();
    }

    private void ShowSettings()
    {
        if (_flow is { IsIdle: false })
        {
            _tray?.ShowBalloon("viewport-cap", "A capture is already in progress.");
            return;
        }

        if (_settings is null || _store is null || _hotkey is null)
            return;

        if (_window is { IsLoaded: true })
        {
            _window.Activate();
            return;
        }

        _window = new SettingsWindow(
            _settings,
            _store,
            _hotkey.CanRegister,
            updated =>
            {
                _settings = updated;
                _tray?.UpdateHotkey(updated.Hotkey);
                _tray?.UpdateStartWithWindows(updated.StartWithWindows);

                try
                {
                    _startup?.SetEnabled(updated.StartWithWindows);
                }
                catch (Exception ex)
                {
                    FileLog.Error("Could not update Start with Windows.", ex);
                    MessageBox.Show(
                        "Could not update the Start with Windows setting.",
                        "viewport-cap",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                if (_hotkey is not null &&
                    HotkeyGesture.TryParse(updated.Hotkey, out var gesture) &&
                    !_hotkey.TryRegister(gesture, OnCapture))
                {
                    MessageBox.Show(
                        $"The hotkey {gesture} is already in use.",
                        "viewport-cap",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            });

        _window.ShowDialog();
        _window = null;
    }

    private void OpenFolder()
    {
        if (_settings is null)
            return;

        Directory.CreateDirectory(_settings.OutputFolder);
        Process.Start(new ProcessStartInfo
        {
            FileName = _settings.OutputFolder,
            UseShellExecute = true
        });
    }
}
