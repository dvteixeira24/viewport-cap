namespace ViewportCap.Core;

public static class ExportNaming
{
    public static string Extension(OutputFormat format) => format switch { OutputFormat.Mp4 => ".mp4", OutputFormat.WebM => ".webm", OutputFormat.Gif => ".gif", _ => throw new ArgumentOutOfRangeException(nameof(format)) };
    public static string BuildBaseName(DateTimeOffset createdAt) => $"viewport-cap_{createdAt:yyyy-MM-dd_HH-mm-ss}";
    public static string EnsureUnique(string directory,string baseName,string extension,Func<string,bool> fileExists)
    {
        var candidate=Path.Combine(directory,baseName+extension); var counter=2;
        while(fileExists(candidate)){candidate=Path.Combine(directory,$"{baseName}-{counter}{extension}");counter++;}
        return candidate;
    }
}
