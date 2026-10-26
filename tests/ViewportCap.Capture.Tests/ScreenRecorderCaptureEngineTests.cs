using System.Reflection;
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Capture.Tests;

public sealed class ScreenRecorderCaptureEngineTests
{
    [Fact]
    public async Task Cancel_WaitsForFinalizationBeforeDeletingRecording()
    {
        var path = Path.Combine(Path.GetTempPath(), $"viewport-cap-{Guid.NewGuid():N}.mp4");
        File.WriteAllText(path, "recording");
        var engine = new ScreenRecorderCaptureEngine();
        var completion = new TaskCompletionSource<CaptureResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        typeof(ScreenRecorderCaptureEngine).GetField("_completion", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(engine, completion);
        typeof(ScreenRecorderCaptureEngine).GetField("_outputPath", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(engine, path);
        var cancel = engine.CancelAsync();
        try
        {
            await Assert.ThrowsAsync<TimeoutException>(() => cancel.WaitAsync(TimeSpan.FromSeconds(6)));
            Assert.True(File.Exists(path));
        }
        finally
        {
            completion.TrySetResult(new CaptureResult(path, false, TimeSpan.FromSeconds(1), 400, 300));
            await cancel;
            if(File.Exists(path))File.Delete(path);
        }
    }
}
