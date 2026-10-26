using System.Runtime.InteropServices;using System.Windows;using System.Windows.Interop;
namespace ViewportCap.App.Services;
public static class WindowAffinity
{
 private const uint WdaExcludeFromCapture=0x11;[DllImport("user32.dll")]private static extern bool SetWindowDisplayAffinity(IntPtr hwnd,uint affinity);
 public static void ExcludeFromCapture(Window w){var h=new WindowInteropHelper(w).Handle;if(h!=IntPtr.Zero)SetWindowDisplayAffinity(h,WdaExcludeFromCapture);}
}
