namespace ViewportCap.Core;

public static class TrimMath
{
    public const double MinGap = 0.25;
    public static double ClampStart(double start, double end, double max)
    {
        start = Math.Clamp(start, 0, max);
        end = Math.Clamp(end, 0, max);
        if (start > end - MinGap) start = Math.Max(0, end - MinGap);
        return start;
    }
    public static double ClampEnd(double start, double end, double max)
    {
        start = Math.Clamp(start, 0, max);
        end = Math.Clamp(end, 0, max);
        if (end < start + MinGap) end = Math.Min(max, start + MinGap);
        return end;
    }
    public static bool TryParseTime(string s, out TimeSpan t)
    {
        if (TimeSpan.TryParseExact(s, @"mm\:ss\.ff", null, out t)) return true;
        if (TimeSpan.TryParseExact(s, @"m\:ss\.ff", null, out t)) return true;
        return TimeSpan.TryParse(s, out t);
    }
    public static string Format(double seconds) => TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.ff");
}
