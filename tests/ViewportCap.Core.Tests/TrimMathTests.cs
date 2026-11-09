using ViewportCap.Core;
using Xunit;
namespace ViewportCap.Core.Tests;
public sealed class TrimMathTests
{
    [Fact] public void Clamp_EnforcesGapAndBounds()
    {
        Assert.Equal(1.0, TrimMath.ClampStart(1.0, 2.0, 10.0));
        Assert.Equal(1.25, TrimMath.ClampEnd(1.0, 1.1, 10.0));
        Assert.Equal(0.0, TrimMath.ClampStart(-5, 5, 10.0));
        Assert.Equal(10.0, TrimMath.ClampEnd(0, 99, 10.0));
    }
    [Fact] public void TryParseTime_ParsesMmSsFf()
    {
        Assert.True(TrimMath.TryParseTime("00:01.50", out var t));
        Assert.Equal(1.5, t.TotalSeconds, 3);
        Assert.False(TrimMath.TryParseTime("junk", out _));
    }
}
