using System.Windows;
using ViewportCap.App.Services;
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.App.Tests;

public sealed class DisplayServiceTests
{
    [Theory]
    [InlineData(0, 0, 1920, 1040, 1800, 100, 96)]
    [InlineData(-1920, 0, 1920, 1040, -100, 1000, 144)]
    [InlineData(1920, -1080, 1920, 1040, 3700, -100, 192)]
    public void NearbyBounds_StayInsideSelectedWorkArea(int left, int top, int width, int height, int x, int y, int dpi)
    {
        var workArea = new Rect(left, top, width, height);
        var region = new CaptureRegion("display", left, top, x, y, 100, 100, dpi);
        var bounds = DisplayService.GetNearbyBounds(region, 300, 112, workArea);

        Assert.True(workArea.Contains(bounds), $"{bounds} is outside {workArea}.");
        Assert.Equal(300 * region.Scale, bounds.Width);
        Assert.Equal(112 * region.Scale, bounds.Height);
    }

    [Fact]
    public void NearbyBounds_ShrinkControlsOnlyWhenWorkAreaIsSmaller()
    {
        var workArea = new Rect(0, 0, 250, 100);
        var region = new CaptureRegion("display", 0, 0, 0, 0, 100, 100, 192);
        Assert.Equal(workArea, DisplayService.GetNearbyBounds(region, 300, 112, workArea));
    }
}
