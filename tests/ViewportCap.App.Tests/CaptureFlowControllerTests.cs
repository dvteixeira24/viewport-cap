using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using ViewportCap.App.Services;
using ViewportCap.App.Windows;
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.App.Tests;

public sealed class CaptureFlowControllerTests
{
    [Fact]
    public Task Shutdown_WhenRecorderCannotStop_ReportsFailureAndKeepsRecording() => StaTest.Run(async () =>
    {
        var engine = new FakeCapture { CancelFailure = new CaptureException("Could not stop the recording.") };
        var flow = CreateFlow(engine);
        flow.StartCapture();
        SelectRegion();
        await engine.Started.Task;
        var path = engine.Request!.OutputPath;
        File.WriteAllText(path, "recording");
        try
        {
            await Assert.ThrowsAsync<CaptureException>(() => flow.ShutdownAsync());
            Assert.Equal("recording", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    });

    [Fact]
    public Task Shutdown_WaitsForRecorderStillFinalizingBehindReview() => StaTest.Run(async () =>
    {
        var engine = new FakeCapture { Cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var flow = CreateFlow(engine);
        flow.StartCapture();
        SelectRegion();
        await engine.Started.Task;
        var path = engine.Request!.OutputPath;
        File.WriteAllBytes(path, new byte[1024]);
        flow.HandleHotkey();
        await engine.StopRequested.Task;
        engine.StopResult.SetException(new TimeoutException());
        await StaTest.Field<Task>(flow, "_captureTask");
        Assert.NotNull(StaTest.Field<ReviewWindow?>(flow, "_review"));

        var shutdown = flow.ShutdownAsync();
        try
        {
            Assert.False(shutdown.IsCompleted, "Review was closed without waiting for the native writer.");
            Assert.True(File.Exists(path));
            await engine.Cancelled.Task;
            engine.Cleanup.TrySetResult();
            await shutdown;
            Assert.False(File.Exists(path));
        }
        finally
        {
            engine.Cleanup.TrySetResult();
            await shutdown;
            if(File.Exists(path))File.Delete(path);
        }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Shutdown_DoesNotOpenReviewAfterFinalizingOrWhileProbing(bool waitingForProbe) => StaTest.Run(async () =>
    {
        var engine = new FakeCapture();
        var probing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probeResult = new TaskCompletionSource<IExporter?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = new CaptureFlowController(() => new AppSettings { CountdownSeconds = 0 }, new MemorySettings(), engine,
            () => { probing.SetResult(); return probeResult.Task; }, _ => { }, () => true);
        flow.StartCapture();
        SelectRegion();
        await engine.Started.Task;
        flow.HandleHotkey();
        await engine.StopRequested.Task;
        var result = new CaptureResult(engine.Request!.OutputPath, false, TimeSpan.FromSeconds(1), 400, 300);
        if(waitingForProbe)
        {
            engine.StopResult.SetResult(result);
            await probing.Task;
        }

        var shutdown = flow.ShutdownAsync();
        if(!waitingForProbe)
        {
            Assert.False(shutdown.IsCompleted);
            engine.StopResult.SetResult(result);
        }
        await shutdown.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.True(flow.IsIdle);
        Assert.Null(StaTest.Field<ReviewWindow?>(flow, "_review"));
        Assert.False(probeResult.Task.IsCompleted);
    });

    [Fact]
    public Task Shutdown_WaitsForRecorderCleanup() => StaTest.Run(async () =>
    {
        var engine = new FakeCapture { Cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var flow = CreateFlow(engine);
        flow.StartCapture();
        SelectRegion();
        await engine.Started.Task;

        var shutdown = flow.ShutdownAsync();
        await engine.Cancelled.Task;
        Assert.False(shutdown.IsCompleted);
        engine.Cleanup.SetResult();
        await shutdown;
        Assert.True(flow.IsIdle);
        flow.StartCapture();
        Assert.True(flow.IsIdle);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Shutdown_ClosesSelectionAndCountdown(bool countdown) => StaTest.Run(async () =>
    {
        var flow = CreateFlow(new FakeCapture(), 10);
        flow.StartCapture();
        if(countdown)
        {
            SelectRegion();
            await System.Windows.Threading.Dispatcher.Yield();
        }
        await flow.ShutdownAsync();
        Assert.True(flow.IsIdle);
        Assert.DoesNotContain(PresentationSource.CurrentSources.OfType<HwndSource>()
            .Select(source => source.RootVisual), window => window is CountdownWindow or RegionOverlayWindow);
    });

    [Fact]
    public Task FailureDuringStart_IsDeliveredToTheRecordingWaiter() => StaTest.Run(async () =>
    {
        var engine = new FakeCapture();
        var flow = CreateFlow(engine);
        var delivered = false;
        engine.OnStart = () =>
        {
            engine.Fail();
            delivered = StaTest.Field<TaskCompletionSource<bool>?>(flow, "_stop")?.Task.IsFaulted == true;
            // Finish this test without opening the interactive error dialog.
            throw new OperationCanceledException();
        };

        flow.StartCapture();
        SelectRegion();
        await engine.Cancelled.Task;

        Assert.True(delivered, "The failure event was lost before the stop waiter existed.");
        Assert.True(flow.IsIdle);
    });

    private static CaptureFlowController CreateFlow(FakeCapture engine, int countdown = 0) => new(
        () => new AppSettings { CountdownSeconds = countdown }, new MemorySettings(), engine,
        () => Task.FromResult<IExporter?>(null), _ => { }, () => true);

    [Fact]
    public Task CancellingCountdown_DoesNotSaveAudioChange() => StaTest.Run(async () =>
    {
        var store = new RecordingSettings();
        var flow = new CaptureFlowController(() => new AppSettings { CountdownSeconds = 10, AudioMode = AudioMode.Off }, store, new FakeCapture(),
            () => Task.FromResult<IExporter?>(null), _ => { }, () => true);
        flow.StartCapture();
        SelectRegion();
        var countdown = await WaitForWindow<CountdownWindow>();
        StaTest.Invoke(countdown, "Audio_Click", countdown, new RoutedEventArgs());
        StaTest.Invoke(countdown, "Cancel_Click", countdown, new RoutedEventArgs());
        for (var i = 0; i < 100 && !flow.IsIdle; i++)
            await System.Windows.Threading.Dispatcher.Yield();
        Assert.True(flow.IsIdle);
        Assert.Equal(0, store.Saves);
    });

    private static async Task<T> WaitForWindow<T>() where T : class
    {
        for (var i = 0; i < 100; i++)
        {
            var found = PresentationSource.CurrentSources.OfType<HwndSource>()
                .Select(source => source.RootVisual).OfType<T>().FirstOrDefault();
            if (found != null) return found;
            await System.Windows.Threading.Dispatcher.Yield();
        }
        throw new TimeoutException($"No {typeof(T).Name} appeared.");
    }

    private static void SelectRegion()
    {
        var overlay = PresentationSource.CurrentSources.OfType<HwndSource>()
            .Select(source => source.RootVisual).OfType<RegionOverlayWindow>().First(window => window.IsHitTestVisible);
        var monitor = overlay.Monitor;
        var selected = (Action<CaptureRegion>)typeof(RegionOverlayWindow)
            .GetField("SelectionCompleted", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(overlay)!;
        selected(new CaptureRegion(monitor.DeviceName, monitor.Left, monitor.Top, monitor.Left + 100, monitor.Top + 100, 400, 300, monitor.Dpi));
    }

    private sealed class FakeCapture : ICaptureEngine
    {
        public event EventHandler<CaptureFailedEventArgs>? Failed;
        public Action? OnStart { get; set; }
        public CaptureRequest? Request { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource StopRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<CaptureResult> StopResult { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource? Cleanup { get; init; }
        public Exception? CancelFailure { get; init; }
        public void Fail() => Failed?.Invoke(this, new CaptureFailedEventArgs("start failed"));
        public Task StartAsync(CaptureRequest request, CancellationToken token) { Request=request; OnStart?.Invoke(); Started.TrySetResult(); return Task.CompletedTask; }
        public Task<CaptureResult> StopAsync(CancellationToken token) { StopRequested.TrySetResult(); return StopResult.Task; }
        public async Task CancelAsync(bool deleteFile = true) { Cancelled.TrySetResult(); if(CancelFailure!=null)throw CancelFailure; if(Cleanup!=null)await Cleanup.Task; }
    }

    private sealed class MemorySettings : ISettingsStore
    {
        public AppSettings Load() => new();
        public void Save(AppSettings settings) { }
    }

    private sealed class RecordingSettings : ISettingsStore
    {
        public int Saves { get; private set; }
        public AppSettings Load() => new();
        public void Save(AppSettings settings) { Saves++; }
    }
}
