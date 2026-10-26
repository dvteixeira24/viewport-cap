using System.Diagnostics;
using System.IO;
using System.Windows;
using ViewportCap.App.Services;
using ViewportCap.App.Windows;
using ViewportCap.Core;
using Application = System.Windows.Application;
namespace ViewportCap.App;
public partial class App:Application
{
 private SingleInstanceService? _single;private HotkeyService? _hotkey;private TrayService? _tray;private StartupService? _startup;private ISettingsStore? _store;private AppSettings? _settings;private SettingsWindow? _window;
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e);AppPaths.EnsureCreated();FileLog.Info("viewport-cap starting.");_single=new();if(!_single.TryAcquire()){Shutdown();return;}_single.Listen(()=>Dispatcher.Invoke(ShowSettings));_store=new JsonSettingsStore(AppPaths.SettingsFile);_settings=_store.Load();_startup=new StartupService();_startup.SetEnabled(_settings.StartWithWindows);_hotkey=new();if(HotkeyGesture.TryParse(_settings.Hotkey,out var gesture))_hotkey.TryRegister(gesture,OnCapture);_tray=new(_settings.Hotkey,_settings.StartWithWindows);_tray.CaptureRequested+=OnCapture;_tray.SettingsRequested+=ShowSettings;_tray.OpenRecordingsRequested+=OpenFolder;_tray.StartWithWindowsChanged+=enabled=>{_settings.StartWithWindows=enabled;_store.Save(_settings);_startup.SetEnabled(enabled);};_tray.ExitRequested+=()=>Shutdown();if(!_settings.HasShownFirstRunTip){_tray.ShowBalloon("viewport-cap",$"Press {_settings.Hotkey} to capture a region.");_settings.HasShownFirstRunTip=true;_store.Save(_settings);}}
 protected override void OnExit(ExitEventArgs e){_tray?.Dispose();_hotkey?.Dispose();_single?.Dispose();base.OnExit(e);}
 private void OnCapture(){FileLog.Info("Capture requested.");}
 private void ShowSettings(){if(_settings==null||_store==null||_hotkey==null)return;if(_window is{IsLoaded:true}){_window.Activate();return;}_window=new(_settings,_store,_hotkey.CanRegister,updated=>{_settings=updated;_tray?.UpdateHotkey(updated.Hotkey);});_window.ShowDialog();_window=null;}
 private void OpenFolder(){if(_settings==null)return;Directory.CreateDirectory(_settings.OutputFolder);Process.Start(new ProcessStartInfo{FileName=_settings.OutputFolder,UseShellExecute=true});}
}
