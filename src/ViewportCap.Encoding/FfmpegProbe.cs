namespace ViewportCap.Encoding;

public static class FfmpegProbe
{
    public static async Task<FfmpegCapabilities> ProbeAsync(
        string ffmpegPath,
        IProcessRunner runner,
        CancellationToken cancellationToken)
    {
        var result = await runner.RunAsync(
            ffmpegPath,
            new[] { "-hide_banner", "-encoders" },
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        return result.ExitCode == 0
            ? FfmpegCapabilities.Parse(result.StandardOutput)
            : new FfmpegCapabilities(false, false, false, false);
    }
}
