using System.IO;
using System.Windows;
using System.Windows.Threading;
using ViewportCap.App.Windows;
using ViewportCap.Capture;
using ViewportCap.Core;
using ViewportCap.Encoding;
using MessageBox = System.Windows.MessageBox;

namespace ViewportCap.App.Services;

public sealed class CaptureFlowController
{
    private readonly Func<AppSettings> _getSettings;
    private readonly ISettingsStore _store;
    private readonly ICaptureEngine _capture;
    private readonly bool _available;
    private readonly IExporter? _exporter;
    private readonly Action<string> _showToast;
    private readonly RecordingStateMachine _state = new();
    private readonly RegionSelector _selector = new();
    private CancellationTokenSource? _cts;
    private TaskCompletionSource<bool>? _stop;
    private RecordingHudWindow? _hud;

    public CaptureFlowController(Func<AppSettings> getSettings, ISettingsStore store, ICaptureEngine capture, IExporter? exporter, Action<string> showToast, bool captureAvailable)
    {
        _getSettings = getSettings;
        _store = store;
        _capture = capture;
        _available = captureAvailable;
        _exporter = exporter;
        _showToast = showToast;
        _capture.Failed += (_, args) => _stop?.TrySetException(new CaptureException(args.Error));
    }

    public bool IsIdle => _state.State == RecordingState.Idle;

    public void HandleHotkey()
    {
        if (_state.State == RecordingState.Recording)
        {
            _stop?.TrySetResult(true);
            return;
        }

        if (IsIdle)
            StartCapture();
    }

    public async void StartCapture()
    {
        if (!IsIdle)
            return;
        if (!_available)
        {
            MessageBox.Show("Screen capture is unavailable. Check the VC++ runtime and Media Foundation.", "viewport-cap", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _state.TransitionTo(RecordingState.Selecting);
        _cts = new CancellationTokenSource();
        try
        {
            var region = await _selector.SelectAsync(_cts.Token);
            if (region is null)
            {
                GoIdle();
                return;
            }

            _state.TransitionTo(RecordingState.Countdown);
            var settings = _getSettings();
            var audio = settings.AudioMode;
            if (settings.CountdownSeconds > 0)
            {
                var countdown = new CountdownWindow(region.Value, settings.CountdownSeconds, audio, mode =>
                {
                    settings.AudioMode = mode;
                    _store.Save(settings);
                });
                var start = await countdown.RunAsync();
                audio = countdown.AudioMode;
                if (!start)
                {
                    GoIdle();
                    return;
                }
            }

            _state.TransitionTo(RecordingState.Recording);
            Directory.CreateDirectory(AppPaths.RecordingsDirectory);
            var path = Path.Combine(AppPaths.RecordingsDirectory, $"{Guid.NewGuid():N}.mp4");
            CapturePreflight.EnsureDiskSpace(path);
            await _capture.StartAsync(new CaptureRequest(region.Value, path, audio, settings.MicrophoneDeviceName, settings.SystemAudioDeviceName), _cts.Token);

            _stop = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _hud = new RecordingHudWindow(region.Value, audio);
            _hud.StopRequested += () => _stop?.TrySetResult(true);
            _hud.CancelRequested += () => _stop?.TrySetResult(false);
            _hud.Start();

            var startedAt = DateTime.UtcNow;
            var longRecordingWarned = false;
            var diskTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            diskTimer.Tick += (_, _) =>
            {
                try
                {
                    var root = Path.GetPathRoot(path);
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        var free = new DriveInfo(root).AvailableFreeSpace;
                        if (free < 250L * 1024 * 1024)
                        {
                            _hud?.ShowWarning("Low disk space. Stopping the recording.");
                            _stop?.TrySetResult(true);
                            diskTimer.Stop();
                        }
                        else if (free < 500L * 1024 * 1024)
                        {
                            _hud?.ShowWarning("Low disk space.");
                        }
                    }
                }
                catch
                {
                    // Disk checks are best effort.
                }

                if (!longRecordingWarned && DateTime.UtcNow - startedAt > TimeSpan.FromMinutes(10))
                {
                    _hud?.ShowWarning("Recording is longer than 10 minutes.");
                    longRecordingWarned = true;
                }
            };
            diskTimer.Start();

            bool stop;
            try
            {
                stop = await _stop.Task;
            }
            finally
            {
                diskTimer.Stop();
            }

            _hud.Close();
            _hud = null;
            _state.TransitionTo(RecordingState.Finalizing);
            if (!stop)
            {
                await _capture.CancelAsync();
                GoIdle();
                return;
            }

            var result = await _capture.StopAsync(CancellationToken.None);
            FileLog.Info($"Recording complete: {result.FilePath} ({result.Duration}).");
            _state.TransitionTo(RecordingState.Reviewing);
            var review = new ReviewWindow(result, settings, _exporter, _showToast);
            review.Closed += (_, _) =>
            {
                if (_state.State == RecordingState.Reviewing)
                    GoIdle();
            };
            review.Show();
        }
        catch (OperationCanceledException)
        {
            try { await _capture.CancelAsync(); } catch { }
            GoIdle();
        }
        catch (Exception ex)
        {
            FileLog.Error("Capture failed.", ex);
            MessageBox.Show($"Capture failed:\n\n{ex.Message}", "viewport-cap");
            try { await _capture.CancelAsync(deleteFile: false); } catch { }
            GoIdle();
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _stop?.TrySetResult(false);
    }

    private void GoIdle()
    {
        _hud?.Close();
        _hud = null;
        _stop = null;
        _cts?.Dispose();
        _cts = null;
        if (_state.State != RecordingState.Idle && _state.CanTransitionTo(RecordingState.Idle))
            _state.TransitionTo(RecordingState.Idle);
    }
}
