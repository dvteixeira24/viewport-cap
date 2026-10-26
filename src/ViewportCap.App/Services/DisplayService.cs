using System.Runtime.InteropServices;using System.Windows;using System.Windows.Interop;using ViewportCap.Core;
namespace ViewportCap.App.Services;
public static class DisplayService
{
 private const uint MonitorDefaultToNearest=2;private const int MdtEffectiveDpi=0;private const uint SwpNoZOrder=0x4;private const uint SwpNoActivate=0x10;
 public static IReadOnlyList<MonitorDescriptor> GetMonitors(){var list=new List<MonitorDescriptor>();EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,(IntPtr h,IntPtr dc,ref NativeRect r,IntPtr d)=>{var i=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};if(GetMonitorInfo(h,ref i))list.Add(new(i.DeviceName,i.Monitor.Left,i.Monitor.Top,i.Monitor.Right-i.Monitor.Left,i.Monitor.Bottom-i.Monitor.Top,Dpi(h),(i.Flags&1)!=0));return true;},IntPtr.Zero);if(list.Count==0)list.Add(new(@"\\.\DISPLAY1",0,0,GetSystemMetrics(0),GetSystemMetrics(1),96,true));return list;}
 public static MonitorDescriptor GetMonitorForCursor(){GetCursorPos(out var p);var h=MonitorFromPoint(p,MonitorDefaultToNearest);var i=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};if(h!=IntPtr.Zero&&GetMonitorInfo(h,ref i))return new(i.DeviceName,i.Monitor.Left,i.Monitor.Top,i.Monitor.Right-i.Monitor.Left,i.Monitor.Bottom-i.Monitor.Top,Dpi(h),(i.Flags&1)!=0);return GetMonitors()[0];}
 public static Rect GetWindowBounds(MonitorDescriptor m){var s=m.Scale;return new Rect(m.Left/s,m.Top/s,m.Width/s,m.Height/s);}
 public static (double Left,double Top) GetNearbyPosition(CaptureRegion r,double w,double h){var bounds=GetNearbyBounds(r,w,h,GetWorkArea(r));return(bounds.Left/r.Scale,bounds.Top/r.Scale);}
 public static Rect GetNearbyBounds(CaptureRegion r,double width,double height,Rect workArea)
 {
  var w=Math.Min(width*r.Scale,workArea.Width);var h=Math.Min(height*r.Scale,workArea.Height);var gap=12*r.Scale;
  var x=r.Right+gap;if(x+w>workArea.Right)x=r.X-w-gap;
  return new Rect(Math.Clamp(x,workArea.Left,workArea.Right-w),Math.Clamp(r.Y,workArea.Top,workArea.Bottom-h),w,h);
 }
 private static Rect GetWorkArea(CaptureRegion r)
 {
  var monitor=MonitorFromPoint(new NativePoint{X=r.X,Y=r.Y},MonitorDefaultToNearest);var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
  if(monitor!=IntPtr.Zero&&GetMonitorInfo(monitor,ref info))return new Rect(info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);
  var fallback=GetMonitors().First(m=>string.Equals(m.DeviceName,r.MonitorDeviceName,StringComparison.OrdinalIgnoreCase));return new Rect(fallback.Left,fallback.Top,fallback.Width,fallback.Height);
 }
 // Left/Top/Width/Height are DIPs scaled by the DPI of the monitor the window is currently on, so cross-DPI placement is only exact in physical pixels (dotnet/wpf#3105).
 public static void SetPhysicalBounds(Window w,double left,double top,double width,double height){var h=new WindowInteropHelper(w).Handle;if(h!=IntPtr.Zero)SetWindowPos(h,IntPtr.Zero,(int)Math.Round(left),(int)Math.Round(top),(int)Math.Round(width),(int)Math.Round(height),SwpNoZOrder|SwpNoActivate);}
 public static void SetNearbyBounds(Window w,CaptureRegion r){var bounds=GetNearbyBounds(r,w.Width,w.Height,GetWorkArea(r));SetPhysicalBounds(w,bounds.Left,bounds.Top,bounds.Width,bounds.Height);}
 private static int Dpi(IntPtr h)=>GetDpiForMonitor(h,MdtEffectiveDpi,out var x,out _)==0&&x>0?(int)x:96;
 private delegate bool MonitorEnumProc(IntPtr m,IntPtr dc,ref NativeRect r,IntPtr d);[DllImport("user32.dll")]private static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorEnumProc cb,IntPtr data);[DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool GetMonitorInfo(IntPtr h,ref MonitorInfo i);[DllImport("user32.dll")]private static extern IntPtr MonitorFromPoint(NativePoint p,uint flags);[DllImport("user32.dll")]private static extern bool GetCursorPos(out NativePoint p);[DllImport("user32.dll")]private static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);[DllImport("user32.dll")]private static extern int GetSystemMetrics(int i);[DllImport("Shcore.dll")]private static extern int GetDpiForMonitor(IntPtr h,int type,out uint x,out uint y);
 [StructLayout(LayoutKind.Sequential)]private struct NativePoint{public int X,Y;}[StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct MonitorInfo{public int Size;public NativeRect Monitor;public NativeRect Work;public int Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string DeviceName;}
}
