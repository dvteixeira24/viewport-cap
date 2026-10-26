using ScreenRecorderLib;
using ViewportCap.Core;
namespace ViewportCap.Capture;
public static class CapturePreflight
{
 public static void EnsureNativeLibraryLoads()=>_ = Recorder.GetDisplays();
 public static void EnsureDiskSpace(string path,long minimumBytes=1_000_000_000){var root=Path.GetPathRoot(path);if(string.IsNullOrWhiteSpace(root))throw new CaptureException("Cannot determine the recording drive.");if(new DriveInfo(root).AvailableFreeSpace<minimumBytes)throw new CaptureException("At least 1 GB of free disk space is required.");}
}
