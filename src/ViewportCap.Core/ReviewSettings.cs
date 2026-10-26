namespace ViewportCap.Core;

public sealed class ReviewSettings
{
    public string SourcePath { get; init; } = string.Empty;
    public string OutputDirectory { get; init; } = string.Empty;
    public OutputFormat Format { get; init; } = OutputFormat.Mp4;
    public QualityPreset Quality { get; init; } = QualityPreset.Balanced;
    public int FrameRate { get; init; } = 30;
    public ScalePreset Scale { get; init; } = ScalePreset.Percent100;
    public TimeSpan TrimStart { get; init; }
    public TimeSpan TrimEnd { get; init; }
    public bool HasAudio { get; init; }
    public bool MuteAudio { get; init; }
    public int SourceWidth { get; init; }
    public int SourceHeight { get; init; }
    public TimeSpan SourceDuration { get; init; }
    public TimeSpan TrimDuration => TrimEnd - TrimStart;
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(SourcePath)) errors.Add("Source path is required.");
        if (string.IsNullOrWhiteSpace(OutputDirectory)) errors.Add("Output directory is required.");
        if (SourceWidth <= 0 || SourceHeight <= 0) errors.Add("Source dimensions must be positive.");
        if (SourceDuration <= TimeSpan.Zero) errors.Add("Source duration must be positive.");
        if (TrimStart < TimeSpan.Zero) errors.Add("Trim start cannot be negative.");
        if (TrimEnd <= TrimStart) errors.Add("Trim end must be after trim start.");
        if (TrimDuration < TimeSpan.FromMilliseconds(250)) errors.Add("The trimmed clip must be at least 0.25 seconds.");
        if (TrimEnd > SourceDuration + TimeSpan.FromMilliseconds(50)) errors.Add("Trim end cannot be after the source duration.");
        if (!Enum.IsDefined(Format)) errors.Add("Output format is invalid.");
        if (!Enum.IsDefined(Quality)) errors.Add("Quality preset is invalid.");
        if (!Enum.IsDefined(Scale)) errors.Add("Scale preset is invalid.");
        if (Format == OutputFormat.Gif) { if (FrameRate is not (10 or 15 or 20)) errors.Add("GIF frame rate must be 10, 15, or 20."); }
        else if (FrameRate is not (15 or 30)) errors.Add("Video frame rate must be 15 or 30.");
        return errors;
    }
}
