namespace ViewportCap.Core;

public static class RegionMath
{
    public static DragRect Normalize(double x1, double y1, double x2, double y2) => new(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    public static CaptureRegion ToPhysical(DragRect local, MonitorDescriptor monitor)
    {
        var scale = monitor.Dpi / 96.0;
        var monitorRight = monitor.Left + monitor.Width; var monitorBottom = monitor.Top + monitor.Height;
        var left = monitor.Left + (int)Math.Round(local.Left * scale, MidpointRounding.AwayFromZero);
        var top = monitor.Top + (int)Math.Round(local.Top * scale, MidpointRounding.AwayFromZero);
        var right = monitor.Left + (int)Math.Round(local.Right * scale, MidpointRounding.AwayFromZero);
        var bottom = monitor.Top + (int)Math.Round(local.Bottom * scale, MidpointRounding.AwayFromZero);
        left = Math.Clamp(left, monitor.Left, monitorRight); top = Math.Clamp(top, monitor.Top, monitorBottom);
        right = Math.Clamp(right, monitor.Left, monitorRight); bottom = Math.Clamp(bottom, monitor.Top, monitorBottom);
        // H.264/CaptureApi need even dimensions; some odd sizes make the recorder fail with "Unspecified error".
        var width = Math.Max(0, right - left) & ~1; var height = Math.Max(0, bottom - top) & ~1;
        return new CaptureRegion(monitor.DeviceName, monitor.Left, monitor.Top, left, top, width, height, monitor.Dpi);
    }
}
