using ViewportCap.Core;using Xunit;
namespace ViewportCap.Core.Tests;
public sealed class CaptureBitrateCalculatorTests
{
 [Fact] public void SmallRegion_UsesMinimumBitrate()=>Assert.Equal(4_000_000,CaptureBitrateCalculator.ForRegion(640,480,30));
 [Fact] public void FullHd_UsesResolutionAwareBitrate()=>Assert.Equal(7_464_960,CaptureBitrateCalculator.ForRegion(1920,1080,30));
 [Fact] public void HugeRegion_UsesMaximumBitrate()=>Assert.Equal(40_000_000,CaptureBitrateCalculator.ForRegion(7680,4320,30));
}
