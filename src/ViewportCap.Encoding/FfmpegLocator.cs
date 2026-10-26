namespace ViewportCap.Encoding;
public static class FfmpegLocator
{
 public static string? Find(string? extraDirectory=null){var candidates=new List<string>{Path.Combine(AppContext.BaseDirectory,"ffmpeg.exe"),Path.Combine(AppContext.BaseDirectory,"tools","ffmpeg.exe")};if(!string.IsNullOrWhiteSpace(extraDirectory))candidates.Add(Path.Combine(extraDirectory,"ffmpeg.exe"));return candidates.FirstOrDefault(File.Exists);}
}
