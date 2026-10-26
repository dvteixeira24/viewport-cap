namespace ViewportCap.Core;

public readonly record struct MonitorDescriptor(string DeviceName, int Left, int Top, int Width, int Height, int Dpi, bool IsPrimary)
{
    public double Scale => Dpi / 96.0;
}
