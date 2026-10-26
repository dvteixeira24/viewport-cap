using System.Runtime.InteropServices;using System.Windows.Interop;using ViewportCap.Core;
namespace ViewportCap.App.Services;
public sealed class HotkeyService:IDisposable
{
 private const int HotkeyId=0x9000,WmHotkey=0x0312;private HwndSource? _source;private Action? _callback;private HotkeyGesture? _current;
 public bool TryRegister(HotkeyGesture gesture,Action callback){EnsureSource();if(_current is not null)UnregisterHotKey(_source!.Handle,HotkeyId);if(!RegisterHotKey(_source!.Handle,HotkeyId,(uint)gesture.Modifiers,gesture.VirtualKey)){if(_current is{} old)RegisterHotKey(_source.Handle,HotkeyId,(uint)old.Modifiers,old.VirtualKey);return false;}_current=gesture;_callback=callback;return true;}
 public bool CanRegister(HotkeyGesture gesture){EnsureSource();if(_current is{} current)UnregisterHotKey(_source!.Handle,HotkeyId);var registered=RegisterHotKey(_source!.Handle,HotkeyId,(uint)gesture.Modifiers,gesture.VirtualKey);if(registered)UnregisterHotKey(_source.Handle,HotkeyId);if(_current is{} previous)RegisterHotKey(_source!.Handle,HotkeyId,(uint)previous.Modifiers,previous.VirtualKey);return registered;}
 public void Unregister(){if(_source!=null)UnregisterHotKey(_source.Handle,HotkeyId);_current=null;}
 public void Dispose(){if(_source==null)return;Unregister();_source.RemoveHook(WndProc);_source.Dispose();_source=null;}
 private void EnsureSource(){if(_source!=null)return;var p=new HwndSourceParameters("ViewportCapHotkey"){Width=0,Height=0,WindowStyle=0,ExtendedWindowStyle=0};_source=new HwndSource(p);_source.AddHook(WndProc);}
 private IntPtr WndProc(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled){if(message==WmHotkey&&wParam.ToInt32()==HotkeyId){_callback?.Invoke();handled=true;}return IntPtr.Zero;}
 [DllImport("user32.dll",SetLastError=true)]private static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint key);[DllImport("user32.dll")]private static extern bool UnregisterHotKey(IntPtr hwnd,int id);
}
