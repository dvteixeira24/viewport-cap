namespace ViewportCap.Core;

public readonly record struct DragRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
}
