using ScreenRecorderLib;
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Capture.Tests;

public sealed class ScreenRecorderOptionsFactoryTests
{
    [Fact]
    public void Create_UsesSoftwareEncodingSoHardwareFinalizationCannotHangStop()
    {
        var region=new CaptureRegion(@"\\.\DISPLAY1",0,0,0,0,1016,562,96);
        var options=ScreenRecorderOptionsFactory.Create(new CaptureRequest(region,"out.mp4",AudioMode.Off,null,null));
        Assert.False(options.VideoEncoderOptions.IsHardwareEncodingEnabled);
        Assert.True(options.VideoEncoderOptions.IsFragmentedMp4Enabled);
    }

    [Fact]
    public void Create_CropsTheRecordingToTheSelectedRegion()
    {
        var region = new CaptureRegion(@"\\.\DISPLAY1", 0, 0, 100, 100, 400, 300, 96);
        var options = ScreenRecorderOptionsFactory.Create(new CaptureRequest(region, "out.mp4", AudioMode.Off, null, null));

        var source = Assert.IsType<DisplayRecordingSource>(Assert.Single(options.SourceOptions.RecordingSources));
        Assert.Equal(@"\\.\DISPLAY1", source.DeviceName);
        var crop = options.OutputOptions.SourceRect;
        Assert.Equal(100, crop.Left);
        Assert.Equal(100, crop.Top);
        Assert.Equal(400, crop.Width);
        Assert.Equal(300, crop.Height);
        // ScreenRecorderLib 7 sizes a single-source canvas from OutputFrameSize and skips SourceRect, recording the whole display.
        Assert.Equal(ScreenSize.Empty, options.OutputOptions.OutputFrameSize);
    }
}
