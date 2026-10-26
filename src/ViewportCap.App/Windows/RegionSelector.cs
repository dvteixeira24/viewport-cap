using ViewportCap.App.Services;using ViewportCap.Core;
namespace ViewportCap.App.Windows;
public sealed class RegionSelector
{
 public async Task<CaptureRegion?> SelectAsync(CancellationToken token){var monitors=DisplayService.GetMonitors();var active=DisplayService.GetMonitorForCursor();var overlays=new List<RegionOverlayWindow>();var done=new TaskCompletionSource<CaptureRegion?>(TaskCreationOptions.RunContinuationsAsynchronously);using var reg=token.Register(()=>done.TrySetResult(null));foreach(var m in monitors){var o=new RegionOverlayWindow(m);o.SelectionCompleted+=r=>done.TrySetResult(r);o.Cancelled+=()=>done.TrySetResult(null);o.Closed+=(_,_)=>done.TrySetResult(null);o.EnableInput(false);overlays.Add(o);o.Show();}overlays.FirstOrDefault(o=>string.Equals(o.Monitor.DeviceName,active.DeviceName,StringComparison.OrdinalIgnoreCase))?.EnableInput(true);var result=await done.Task;foreach(var o in overlays){try{o.Close();}catch{}}return result;}
}
