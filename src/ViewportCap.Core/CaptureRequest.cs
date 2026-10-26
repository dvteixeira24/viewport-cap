namespace ViewportCap.Core;
public sealed record CaptureRequest(CaptureRegion Region,string OutputPath,AudioMode AudioMode,string? MicrophoneDeviceName,string? SystemAudioDeviceName);
