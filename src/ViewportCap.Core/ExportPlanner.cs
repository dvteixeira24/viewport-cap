namespace ViewportCap.Core;

public static class ExportPlanner
{
    public static ExportPlan Create(ReviewSettings settings,DateTimeOffset createdAt,Func<string,bool> fileExists)
    {
        ValidateOrThrow(settings); var extension=ExportNaming.Extension(settings.Format); var name=ExportNaming.BuildBaseName(createdAt);
        var path=ExportNaming.EnsureUnique(settings.OutputDirectory,name,extension,fileExists);
        return new ExportPlan(settings,path,CanCopySource(settings));
    }
    public static ExportPlan CreateForPath(ReviewSettings settings,string outputPath)
    {
        ValidateOrThrow(settings); if(string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.",nameof(outputPath));
        if(string.Equals(Path.GetFullPath(outputPath),Path.GetFullPath(settings.SourcePath),StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Output path must not be the temporary recording.",nameof(outputPath));
        return new ExportPlan(settings,outputPath,CanCopySource(settings));
    }
    public static bool CanCopySource(ReviewSettings s)=>s.Format==OutputFormat.Mp4&&s.Quality==QualityPreset.Balanced&&s.FrameRate==30&&s.Scale==ScalePreset.Percent100&&s.TrimStart<=TimeSpan.FromMilliseconds(50)&&s.TrimEnd>=s.SourceDuration-TimeSpan.FromMilliseconds(50)&&(!s.HasAudio||!s.MuteAudio);
    private static void ValidateOrThrow(ReviewSettings settings){var errors=settings.Validate();if(errors.Count>0)throw new ArgumentException(string.Join(" ",errors),nameof(settings));}
}
