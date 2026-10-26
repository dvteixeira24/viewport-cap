using Microsoft.Win32;
namespace ViewportCap.App.Services;
public sealed class StartupService
{
 private const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";private const string ValueName="viewport-cap";
 public bool IsEnabled{get{using var k=Registry.CurrentUser.OpenSubKey(RunKey);return k?.GetValue(ValueName) is string;}}
 public void SetEnabled(bool enabled){using var k=Registry.CurrentUser.CreateSubKey(RunKey,true);if(enabled){var exe=Environment.ProcessPath??throw new InvalidOperationException("Cannot determine the executable path.");k.SetValue(ValueName,$"\"{exe}\" --tray");}else k.DeleteValue(ValueName,false);}
}
