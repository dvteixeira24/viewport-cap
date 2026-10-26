using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Encoding.Tests;

public sealed class ExportFileSafetyTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "viewport-cap-tests", Guid.NewGuid().ToString("N"));

    public ExportFileSafetyTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, true);

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Export_DoesNotTouchAnExistingPartialFile(bool copy, bool fail)
    {
        var source = Path.Combine(_directory, "source.mp4");
        var output = Path.Combine(_directory, "demo.mp4");
        var unrelated = Path.Combine(_directory, "demo.partial.mp4");
        File.WriteAllText(source, "recording");
        File.WriteAllText(unrelated, "user data");
        var settings = new ReviewSettings { SourcePath = source };
        var exporter = new FfmpegExporter("ffmpeg.exe", new(true, true, true, true), new Runner(fail));

        if (fail)
            await Assert.ThrowsAsync<ExportException>(() => exporter.ExportAsync(new(settings, output, copy), null, CancellationToken.None));
        else
            await exporter.ExportAsync(new(settings, output, copy), null, CancellationToken.None);

        Assert.Equal("user data", File.ReadAllText(unrelated));
        Assert.Equal(fail ? 2 : 3, Directory.GetFiles(_directory).Length);
    }

    [Fact]
    public async Task CancelledCopy_PreservesSourceAndExistingDestination()
    {
        var source = Path.Combine(_directory, "source.mp4");
        var output = Path.Combine(_directory, "demo.mp4");
        File.WriteAllText(source, "recording");
        File.WriteAllText(output, "existing export");
        var exporter = new FfmpegExporter("ffmpeg.exe", new(true, true, true, true), new Runner(false));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            exporter.ExportAsync(new(new ReviewSettings { SourcePath = source }, output, true), null, cts.Token));

        Assert.Equal("existing export", File.ReadAllText(output));
        Assert.Equal("recording", File.ReadAllText(source));
        Assert.Equal(2, Directory.GetFiles(_directory).Length);
    }

    [Fact]
    public void PartialPath_IsUniqueAndKeepsTheOutputExtension()
    {
        var output = Path.Combine(_directory, "demo.webm");
        var first = ExportFiles.PartialPath(output);
        Assert.NotEqual(first, ExportFiles.PartialPath(output));
        Assert.Equal(_directory, Path.GetDirectoryName(first));
        Assert.Equal(".webm", Path.GetExtension(first));
    }

    private sealed class Runner(bool fail) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, Action<string>? stdout, Action<string>? stderr, CancellationToken token)
        {
            File.WriteAllText(arguments[^1], "encoded");
            return Task.FromResult(new ProcessResult(fail ? 1 : 0, "", "failed"));
        }
    }
}
