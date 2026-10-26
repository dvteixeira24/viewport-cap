using System.IO;
namespace ViewportCap.App.Services;
public static class FileLog
{
 private static readonly object Gate=new();private const long MaxBytes=2*1024*1024;
 public static void Info(string m)=>Write("INFO",m,null);public static void Warn(string m)=>Write("WARN",m,null);public static void Error(string m,Exception? e=null)=>Write("ERROR",m,e);
 private static void Write(string level,string message,Exception? exception){try{lock(Gate){AppPaths.EnsureCreated();RotateIfNeeded();var line=$"{DateTimeOffset.Now:O} [{level}] {message}";if(exception!=null)line+=Environment.NewLine+exception;File.AppendAllText(AppPaths.LogFile,line+Environment.NewLine);}}catch{}}
 private static void RotateIfNeeded(){var f=new FileInfo(AppPaths.LogFile);if(!f.Exists||f.Length<MaxBytes)return;for(var i=4;i>=1;i--){var from=i==1?AppPaths.LogFile:Path.Combine(AppPaths.LogsDirectory,$"viewport-cap.{i}.log");var to=Path.Combine(AppPaths.LogsDirectory,$"viewport-cap.{i+1}.log");if(File.Exists(from))File.Move(from,to,true);}}
}
