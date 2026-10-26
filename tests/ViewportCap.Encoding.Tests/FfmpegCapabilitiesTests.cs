using ViewportCap.Encoding;
using Xunit;
namespace ViewportCap.Encoding.Tests;
public sealed class FfmpegCapabilitiesTests
{
    [Fact] public void Parse_FindsSupportedEncoders(){var c=FfmpegCapabilities.Parse(" V..... h264_mf Media Foundation\n V..... libopenh264 OpenH264\n V..... libvpx-vp9 VP9\n A..... libopus Opus\n");Assert.True(c.HasH264Mf);Assert.True(c.HasOpenH264);Assert.True(c.HasVp9);Assert.True(c.HasOpus);Assert.True(c.CanEncodeH264);}
    [Fact] public void Parse_ReturnsFalseWhenEncodersMissing(){var c=FfmpegCapabilities.Parse(" V..... h264_mf\n");Assert.True(c.HasH264Mf);Assert.False(c.HasOpenH264);Assert.False(c.HasVp9);Assert.False(c.HasOpus);}
}
