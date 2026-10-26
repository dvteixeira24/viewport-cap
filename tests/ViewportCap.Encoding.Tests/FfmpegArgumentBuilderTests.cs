using ViewportCap.Core;using ViewportCap.Encoding;using Xunit;
namespace ViewportCap.Encoding.Tests;
public sealed class FfmpegArgumentBuilderTests
{
 private static readonly FfmpegCapabilities All=new(true,true,true,true);
 [Fact] public void Mp4_ContainsH264AndFastStart(){var a=FfmpegArgumentBuilder.Build(Settings(),@"C:\out\clip.mp4",All);Assert.Contains("h264_mf",a);Assert.Contains("-vf",a);Assert.Contains("+faststart",a);Assert.Contains("-an",a);Assert.Equal(@"C:\out\clip.mp4",a[^1]);}
 [Fact] public void WebM_ContainsVp9AndOpusWhenAudioKept(){var a=FfmpegArgumentBuilder.Build(Settings(OutputFormat.WebM,30,true),@"C:\out\clip.webm",All);Assert.Contains("libvpx-vp9",a);Assert.Contains("libopus",a);Assert.Contains("0:a:0?",a);Assert.DoesNotContain("-an",a);}
 [Fact] public void Mp4_WithAudio_MapsVideoAndAudio(){var a=FfmpegArgumentBuilder.Build(Settings(OutputFormat.Mp4,30,true),@"C:\out\clip.mp4",All);Assert.Contains("0:v:0",a);Assert.Contains("0:a:0?",a);Assert.DoesNotContain("-an",a);}
 [Fact] public void Gif_WithAudio_StillDropsAudio(){var a=FfmpegArgumentBuilder.Build(Settings(OutputFormat.Gif,15,true),@"C:\out\clip.gif",All);Assert.Contains("-an",a);Assert.DoesNotContain("0:a:0?",a);}
 [Fact] public void Gif_ContainsPaletteFilterAndNoAudio(){var a=FfmpegArgumentBuilder.Build(Settings(OutputFormat.Gif,15),@"C:\out\clip.gif",All);Assert.Contains("-filter_complex",a);Assert.Contains(a,x=>x.Contains("palettegen=max_colors=128"));Assert.Contains("-loop",a);Assert.Contains("-an",a);}
 [Fact] public void Trim_UsesAccurateTimecodes(){var s=Settings();s=Copy(s,TimeSpan.FromSeconds(1.5),TimeSpan.FromSeconds(3.75));var a=FfmpegArgumentBuilder.Build(s,@"C:\out\clip.mp4",All);Assert.Contains("00:00:01.500",a);Assert.Contains("00:00:02.250",a);}
 [Fact] public void Mp4_ThrowsWhenNoH264Encoder()=>Assert.Throws<InvalidOperationException>(()=>FfmpegArgumentBuilder.Build(Settings(),@"C:\out\x.mp4",new(false,false,true,true)));
 private static ReviewSettings Settings(OutputFormat f=OutputFormat.Mp4,int fps=30,bool audio=false)=>new(){SourcePath=@"C:\temp\source.mp4",OutputDirectory=@"C:\out",Format=f,Quality=QualityPreset.Balanced,FrameRate=fps,Scale=f==OutputFormat.Gif?ScalePreset.Fit960:ScalePreset.Percent100,TrimEnd=TimeSpan.FromSeconds(5),HasAudio=audio,SourceWidth=1280,SourceHeight=720,SourceDuration=TimeSpan.FromSeconds(5)};
 private static ReviewSettings Copy(ReviewSettings s,TimeSpan start,TimeSpan end)=>new(){SourcePath=s.SourcePath,OutputDirectory=s.OutputDirectory,Format=s.Format,Quality=s.Quality,FrameRate=s.FrameRate,Scale=s.Scale,TrimStart=start,TrimEnd=end,SourceWidth=s.SourceWidth,SourceHeight=s.SourceHeight,SourceDuration=s.SourceDuration};
}
