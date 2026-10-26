using ViewportCap.Encoding;
using Xunit;
namespace ViewportCap.Encoding.Tests;
public sealed class FfmpegProgressParserTests
{
    [Fact] public void Feed_ParsesOutTimeMicroseconds(){var p=new FfmpegProgressParser(10);p.Feed("out_time_us=5000000");Assert.Equal(50,p.Percent);}
    [Fact] public void Feed_ParsesEndMarker(){var p=new FfmpegProgressParser(10);p.Feed("progress=end");Assert.Equal(100,p.Percent);}
    [Fact] public void Feed_IgnoresInvalidLines(){var p=new FfmpegProgressParser(10);p.Feed("out_time_us=not-a-number");Assert.Null(p.Percent);}
}
