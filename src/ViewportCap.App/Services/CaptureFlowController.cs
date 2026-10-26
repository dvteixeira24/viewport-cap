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
    private readonly Func<bool> _isAvailable;
    private readonly Func<Task<IExporter?>> _exporterProvider;
    private readonly Action<string> _showToast;
    private readonly RecordingStateMachine _state = new();
    private readonly RegionSelector _selector = new();
    private CancellationTokenSource? _cts;
    private TaskCompletionSource<bool>? _stop;
    private RecordingHudWindow? _hud;
    private ReviewWindow? _review;

    public CaptureFlowController(Func<AppSettings> getSettings, ISettingsStore store, ICaptureEngine capture, Func<Task<IExporter?>> exporterProvider, Action<string> showToast, Func<bool> isAvailable)
    {
        _getSettings = getSettings;
        _store = store;
        _capture = capture;
        _exporterProvider = exporterProvider;
        _showToast = showToast;
        _isAvailable = isAvailable;
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
        if (!_isAvailable())
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
                    try
                    {
                        settings.AudioMode = mode;
                        _store.Save(settings);
                    }
                    catch (Exception ex)
                    {
                        FileLog.Error("Could not save the audio mode.", ex);
                    }
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

            var stopRequestedAt = DateTime.UtcNow;
            CaptureResult result;
            try
            {
                result = await _capture.StopAsync(CancellationToken.None);
            }
            catch (TimeoutException ex) when (HasPlausibleFile(path))
            {
                // Finalisation is still running; the recorder keeps writing and the file is kept, so review it instead of failing.
                FileLog.Error("Recording finalisation is slow; keeping the file for review.", ex);
                _showToast("Finalising is taking longer. The recording is in Review.");
                result = new CaptureResult(path, audio != AudioMode.Off, stopRequestedAt - startedAt, region.Value.Width, region.Value.Height);
            }

            FileLog.Info($"Recording complete: {result.FilePath} ({result.Duration}).");
            _state.TransitionTo(RecordingState.Reviewing);
            // Cap the wait so a hung probe cannot keep Review closed; the probe keeps running in the background.
            var exporterInit = _exporterProvider();
            var exporter = await Task.WhenAny(exporterInit, Task.Delay(TimeSpan.FromSeconds(30))) == exporterInit
                ? await exporterInit
                : null;
            var review = new ReviewWindow(result, settings, exporter, _showToast);
            _review = review;
            review.Closed += (_, _) =>
            {
                _review = null;
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
        _review?.CancelExport();
    }

    // MP4 headers alone are about 1 KB; a smaller file is not playable and not worth reviewing.
    private static bool HasPlausibleFile(string path)
    {
        try
        {
            var file = new FileInfo(path);
            return file.Exists && file.Length >= 1024;
        }
        catch
        {
            return false;
        }
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
