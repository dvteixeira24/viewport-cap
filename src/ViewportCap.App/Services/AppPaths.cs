using System.IO;
namespace ViewportCap.App.Services;
public static class AppPaths
{
 public static string Root{get;}=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ViewportCap");
 public static string SettingsFile=>Path.Combine(Root,"settings.json");public static string RecordingsDirectory=>Path.Combine(Root,"recordings");public static string LogsDirectory=>Path.Combine(Root,"logs");public static string LogFile=>Path.Combine(LogsDirectory,"viewport-cap.log");
 public static void EnsureCreated(){Directory.CreateDirectory(Root);Directory.CreateDirectory(RecordingsDirectory);Directory.CreateDirectory(LogsDirectory);}
 public static void CleanupOldRecordings(TimeSpan maxAge){if(!Directory.Exists(RecordingsDirectory))return;var cutoff=DateTime.UtcNow-maxAge;foreach(var file in Directory.EnumerateFiles(RecordingsDirectory,"*.mp4")){try{if(File.GetLastWriteTimeUtc(file)<cutoff)File.Delete(file);}catch{}}}
}
