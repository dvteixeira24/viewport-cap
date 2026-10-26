namespace ViewportCap.Encoding;
public static class FfmpegProbe
{
 public static async Task<FfmpegCapabilities> ProbeAsync(string ffmpegPath,IProcessRunner runner,CancellationToken cancellationToken){var r=await runner.RunAsync(ffmpegPath,new[]{"-hide_banner","-encoders"},null,null,cancellationToken);return r.ExitCode==0?FfmpegCapabilities.Parse(r.StandardOutput):new(false,false,false,false);}
}
