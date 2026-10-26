namespace ViewportCap.Core;

public readonly record struct CaptureRegion(string MonitorDeviceName, int MonitorLeft, int MonitorTop, int X, int Y, int Width, int Height, int Dpi)
{
    public const int MinimumSize = 32;
    public bool IsValid => Width >= MinimumSize && Height >= MinimumSize;
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public int SourceX => X - MonitorLeft;
    public int SourceY => Y - MonitorTop;
    public double Scale => Dpi / 96.0;
}
