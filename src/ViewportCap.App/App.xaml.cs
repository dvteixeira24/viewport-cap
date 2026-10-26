using System.Diagnostics;
using System.IO;
using System.Windows;
using ViewportCap.App.Services;
using ViewportCap.App.Windows;
using ViewportCap.Core;
using ViewportCap.Capture;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
namespace ViewportCap.App;
public partial class App:Application
{
 private SingleInstanceService? _single;private HotkeyService? _hotkey;private TrayService? _tray;private StartupService? _startup;private ISettingsStore? _store;private AppSettings? _settings;private SettingsWindow? _window;private ScreenRecorderCaptureEngine? _capture;private CaptureFlowController? _flow;private bool _captureAvailable;
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e);AppPaths.EnsureCreated();FileLog.Info("viewport-cap starting.");_single=new();if(!_single.TryAcquire()){Shutdown();return;}_single.Listen(()=>Dispatcher.Invoke(ShowSettings));_store=new JsonSettingsStore(AppPaths.SettingsFile);_settings=_store.Load();_startup=new StartupService();_startup.SetEnabled(_settings.StartWithWindows);_hotkey=new();if(HotkeyGesture.TryParse(_settings.Hotkey,out var gesture))_hotkey.TryRegister(gesture,OnCapture);_capture=new();try{CapturePreflight.EnsureNativeLibraryLoads();_captureAvailable=true;}catch(Exception ex){_captureAvailable=false;FileLog.Error("Screen capture prerequisites are missing.",ex);MessageBox.Show("Screen capture is unavailable. Install the Microsoft Visual C++ runtime and Media Feature Pack if required.","viewport-cap",MessageBoxButton.OK,MessageBoxImage.Error);}_flow=new(()=>_settings!,_store,_capture,_captureAvailable);_tray=new(_settings.Hotkey,_settings.StartWithWindows);_tray.CaptureRequested+=OnCapture;_tray.SettingsRequested+=ShowSettings;_tray.OpenRecordingsRequested+=OpenFolder;_tray.StartWithWindowsChanged+=enabled=>{_settings.StartWithWindows=enabled;_store.Save(_settings);_startup.SetEnabled(enabled);};_tray.ExitRequested+=()=>{if(_flow is{IsIdle:false}){if(MessageBox.Show("A capture is in progress. Exit anyway?","viewport-cap",MessageBoxButton.YesNo)==MessageBoxResult.Yes)_flow.Cancel();else return;}Shutdown();};if(!_settings.HasShownFirstRunTip){_tray.ShowBalloon("viewport-cap",$"Press {_settings.Hotkey} to capture a region.");_settings.HasShownFirstRunTip=true;_store.Save(_settings);}}
 protected override void OnExit(ExitEventArgs e){_tray?.Dispose();_hotkey?.Dispose();_single?.Dispose();base.OnExit(e);}
 private void OnCapture(){_flow?.HandleHotkey();}
 private void ShowSettings(){if(_flow is{IsIdle:false}){_tray?.ShowBalloon("viewport-cap","A capture is already in progress.");return;}if(_settings==null||_store==null||_hotkey==null)return;if(_window is{IsLoaded:true}){_window.Activate();return;}_window=new(_settings,_store,_hotkey.CanRegister,updated=>{_settings=updated;_tray?.UpdateHotkey(updated.Hotkey);});_window.ShowDialog();_window=null;}
 private void OpenFolder(){if(_settings==null)return;Directory.CreateDirectory(_settings.OutputFolder);Process.Start(new ProcessStartInfo{FileName=_settings.OutputFolder,UseShellExecute=true});}
}
