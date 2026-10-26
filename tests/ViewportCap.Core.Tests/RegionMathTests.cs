using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class RegionMathTests
{
    [Fact] public void Normalize_HandlesReversedDrag() { var rect = RegionMath.Normalize(300, 200, 100, 50); Assert.Equal(100, rect.Left); Assert.Equal(50, rect.Top); Assert.Equal(200, rect.Width); Assert.Equal(150, rect.Height); }
    [Fact] public void ToPhysical_ScalesAt150Percent() { var m = new MonitorDescriptor(@"\\.\DISPLAY1", 0, 0, 1920, 1080, 144, true); var r = RegionMath.ToPhysical(new DragRect(100, 50, 400, 200), m); Assert.Equal(150, r.X); Assert.Equal(75, r.Y); Assert.Equal(600, r.Width); Assert.Equal(300, r.Height); Assert.Equal(144, r.Dpi); Assert.True(r.IsValid); }
    [Fact] public void ToPhysical_ClampsToMonitor() { var m = new MonitorDescriptor(@"\\.\DISPLAY2", -1920, 0, 1920, 1080, 96, false); var r = RegionMath.ToPhysical(new DragRect(-100, -50, 3000, 1500), m); Assert.Equal(-1920, r.X); Assert.Equal(0, r.Y); Assert.Equal(1920, r.Width); Assert.Equal(1080, r.Height); }
    [Fact] public void CaptureRegion_ComputesSourceCoordinates() { var r = new CaptureRegion(@"\\.\DISPLAY1", 1920, 0, 2000, 100, 400, 300, 120); Assert.Equal(80, r.SourceX); Assert.Equal(100, r.SourceY); Assert.Equal(2400, r.Right); Assert.Equal(400, r.Bottom); Assert.Equal(1.25, r.Scale); }
}
