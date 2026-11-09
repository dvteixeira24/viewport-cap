using System.IO;
using ViewportCap.App.Windows;
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.App.Tests;

public sealed class ReviewWindowTests
{
    [Fact]
    public Task Shutdown_WaitsForExportCancellationCleanup() => StaTest.Run(async () =>
    {
        var exporter = new BlockingExporter();
        var recording = new CaptureResult(Path.Combine(Path.GetTempPath(), "viewport-cap-missing.mp4"), false, TimeSpan.FromSeconds(5), 640, 480);
        var window = new ReviewWindow(recording, new AppSettings { OutputFolder = Path.GetTempPath() }, exporter, _ => { });
        var closed = false;
        window.Closed += (_, _) => closed = true;
        var export = (Task)StaTest.Invoke(window, "ExportAsync", Path.Combine(Path.GetTempPath(), "viewport-cap-test-output.mp4"))!;
        await exporter.Started.Task;

        var shutdown = window.CloseForShutdownAsync();
        await exporter.Cancelled.Task;
        Assert.False(shutdown.IsCompleted);
        Assert.False(closed);
        exporter.Cleanup.SetResult();
        await shutdown;
        await export;
        Assert.True(closed);
    });

    [Fact]
    public Task MissingFfmpeg_WithGifDefault_AllowsUnchangedMp4Copy() => StaTest.Run(() =>
    {
        var recording = new CaptureResult(Path.Combine(Path.GetTempPath(), "viewport-cap-missing.mp4"), true, TimeSpan.FromSeconds(5), 640, 480);
        var window = new ReviewWindow(recording, new AppSettings { DefaultFormat = OutputFormat.Gif, OutputFolder = Path.GetTempPath() }, null, _ => { });
        try
        {
            var settings = (ReviewSettings)StaTest.Invoke(window, "BuildSettings")!;
            Assert.Empty(settings.Validate());
            Assert.True(ExportPlanner.CanCopySource(settings));
            Assert.Equal(30, settings.FrameRate);
            Assert.Equal(ScalePreset.Percent100, settings.Scale);
        }
        finally
        {
            window.GetType().GetField("_deleted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            window.Close();
        }
        return Task.CompletedTask;
    });

    [Fact]
    public Task Export_ProbeBuildSettings() => StaTest.Run(() =>
    {
        var recording = new CaptureResult(Path.Combine(Path.GetTempPath(), "viewport-cap-missing.mp4"), false, TimeSpan.FromSeconds(5), 640, 480);
        var window = new ReviewWindow(recording, new AppSettings { OutputFolder = Path.GetTempPath() }, new BlockingExporter(), _ => { });
        try
        {
            var settings = (ReviewSettings)StaTest.Invoke(window, "BuildSettings")!;
            var errors = settings.Validate();
            Assert.Empty(errors);
        }
        finally
        {
            window.GetType().GetField("_deleted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            window.Close();
        }
        return Task.CompletedTask;
    });

    [Fact]
    public Task Timeline_InitializesFullRange() => StaTest.Run(() =>
    {
        var recording = new CaptureResult(Path.Combine(Path.GetTempPath(), "viewport-cap-missing.mp4"), false, TimeSpan.FromSeconds(5), 640, 480);
        var window = new ReviewWindow(recording, new AppSettings { OutputFolder = Path.GetTempPath() }, new BlockingExporter(), _ => { });
        try
        {
            var timeline = (TrimTimeline)window.FindName("Timeline");
            Assert.NotNull(timeline);
            Assert.Equal(0, timeline.Start);
            Assert.Equal(5, timeline.End);
            Assert.Equal(5, timeline.Duration);
            var settings = (ReviewSettings)StaTest.Invoke(window, "BuildSettings")!;
            Assert.Equal(TimeSpan.Zero, settings.TrimStart);
            Assert.Equal(TimeSpan.FromSeconds(5), settings.TrimEnd);
        }
        finally
        {
            window.GetType().GetField("_deleted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            window.Close();
        }
        return Task.CompletedTask;
    });

    [Fact]
    public Task Export_ShowsCancelDuringExport() => StaTest.Run(async () =>
    {
        var exporter = new BlockingExporter();
        var recording = new CaptureResult(Path.Combine(Path.GetTempPath(), "viewport-cap-missing.mp4"), false, TimeSpan.FromSeconds(5), 640, 480);
        var window = new ReviewWindow(recording, new AppSettings { OutputFolder = Path.GetTempPath() }, exporter, _ => { });
        try
        {
            var export = (Task)StaTest.Invoke(window, "ExportAsync", Path.Combine(Path.GetTempPath(), "viewport-cap-test-output.mp4"))!;
            await exporter.Started.Task;
            var cancel = (System.Windows.Controls.Button)window.FindName("CancelButton");
            Assert.NotNull(cancel);
            Assert.Equal(System.Windows.Visibility.Visible, cancel.Visibility);
            Assert.True(cancel.IsEnabled);
            window.CancelExport();
            exporter.Cleanup.SetResult();
            await export;
        }
        finally
        {
            window.CancelExport();
            exporter.Cleanup.TrySetResult();
            window.GetType().GetField("_deleted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            try { await window.CloseForShutdownAsync(); } catch { }
        }
    });

    private sealed class BlockingExporter : IExporter
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cleanup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<ExportResult> ExportAsync(ExportPlan plan, IProgress<double>? progress, CancellationToken token)
        {
            Started.SetResult();
            try { await Task.Delay(Timeout.Infinite, token); }
            catch(OperationCanceledException) { Cancelled.SetResult(); await Cleanup.Task; throw; }
            throw new InvalidOperationException("Expected cancellation.");
        }
    }
}
