using System.IO;
namespace ViewportCap.App.Services;
public static class AppPaths
{
 public static string Root{get;}=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ViewportCap");
 public static string SettingsFile=>Path.Combine(Root,"settings.json");public static string RecordingsDirectory=>Path.Combine(Root,"recordings");public static string LogsDirectory=>Path.Combine(Root,"logs");public static string LogFile=>Path.Combine(LogsDirectory,"viewport-cap.log");
 public static void EnsureCreated(){Directory.CreateDirectory(Root);Directory.CreateDirectory(RecordingsDirectory);Directory.CreateDirectory(LogsDirectory);}
}
