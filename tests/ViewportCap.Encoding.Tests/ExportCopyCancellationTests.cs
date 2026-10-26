using System.IO.Pipes;
using Xunit;

namespace ViewportCap.Encoding.Tests;

public sealed class ExportCopyCancellationTests
{
    [Fact]
    public async Task CancelDuringCopy_ClosesStreamsAndPreservesExistingOutput()
    {
        var directory = Path.Combine(Path.GetTempPath(), "viewport-cap-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var output = Path.Combine(directory, "out.mp4");
        File.WriteAllText(output, "existing export");
        var pipeName = $"viewport-cap-{Guid.NewGuid():N}";
        await using var source = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        using var cancellation = new CancellationTokenSource();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var copy = ExportFiles.CopyIntoPlaceAsync($@"\\.\pipe\{pipeName}", output, cancellation.Token);
        try
        {
            // The source stays open without finishing, so cancellation is tested during I/O, not before it starts.
            await source.WaitForConnectionAsync(timeout.Token);
            await source.WriteAsync(new byte[] { 1, 2, 3 }, timeout.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => copy.WaitAsync(timeout.Token));
            Assert.Equal("existing export", File.ReadAllText(output));
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            cancellation.Cancel();
            await source.DisposeAsync();
            try { await copy; } catch (OperationCanceledException) { }
            Directory.Delete(directory, true);
        }
    }
}
