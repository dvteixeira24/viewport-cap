namespace ViewportCap.Core;
public sealed record CaptureResult(string FilePath,bool HasAudio,TimeSpan Duration,int Width,int Height);
