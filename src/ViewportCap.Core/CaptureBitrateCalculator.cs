namespace ViewportCap.Core;
public static class CaptureBitrateCalculator
{
 public const int MinimumBitrate=4_000_000;public const int MaximumBitrate=40_000_000;public const double BitsPerPixel=.12;
 public static int ForRegion(int width,int height,int framesPerSecond){var value=width*(double)height*framesPerSecond*BitsPerPixel;return(int)Math.Clamp(value,MinimumBitrate,MaximumBitrate);}
}
