using ViewportCap.Core;
using Xunit;
namespace ViewportCap.Core.Tests;
public sealed class ExportPlannerTests
{
    [Fact] public void Create_UsesTimestampedNameAndCopyFastPath(){var p=ExportPlanner.Create(Valid(),new DateTimeOffset(2026,10,26,14,32,5,TimeSpan.Zero),_=>false);Assert.Equal(@"C:\out\viewport-cap_2026-10-26_14-32-05.mp4",p.OutputPath);Assert.True(p.CopySource);}
    [Fact] public void Create_ReencodesWhenTrimmed()=>Assert.False(ExportPlanner.Create(Copy(Valid(),trimStart:TimeSpan.FromSeconds(1),trimEnd:TimeSpan.FromSeconds(4)),DateTimeOffset.UtcNow,_=>false).CopySource);
    [Fact] public void Create_ReencodesForGif()=>Assert.False(ExportPlanner.Create(Copy(Valid(),format:OutputFormat.Gif,frameRate:15,scale:ScalePreset.Fit960),DateTimeOffset.UtcNow,_=>false).CopySource);
    [Fact] public void Create_ThrowsForInvalidSettings()=>Assert.Throws<ArgumentException>(()=>ExportPlanner.Create(Copy(Valid(),trimEnd:TimeSpan.FromMilliseconds(100)),DateTimeOffset.UtcNow,_=>false));
    [Fact] public void CreateForPath_ThrowsWhenOutputIsTheSourceRecording()=>Assert.Throws<ArgumentException>(()=>ExportPlanner.CreateForPath(Valid(), Valid().SourcePath));
    [Fact] public void Validate_RejectsThirtyFpsGif()=>Assert.Contains(Copy(Valid(),format:OutputFormat.Gif,frameRate:30,scale:ScalePreset.Fit960).Validate(),e=>e.Contains("GIF",StringComparison.OrdinalIgnoreCase));
    private static ReviewSettings Valid()=>new(){SourcePath=@"C:\temp\source.mp4",OutputDirectory=@"C:\out",Format=OutputFormat.Mp4,Quality=QualityPreset.Balanced,FrameRate=30,Scale=ScalePreset.Percent100,TrimStart=TimeSpan.Zero,TrimEnd=TimeSpan.FromSeconds(5),HasAudio=false,MuteAudio=false,SourceWidth=1280,SourceHeight=720,SourceDuration=TimeSpan.FromSeconds(5)};
    private static ReviewSettings Copy(ReviewSettings s,OutputFormat? format=null,QualityPreset? quality=null,int? frameRate=null,ScalePreset? scale=null,TimeSpan? trimStart=null,TimeSpan? trimEnd=null)=>new(){SourcePath=s.SourcePath,OutputDirectory=s.OutputDirectory,Format=format??s.Format,Quality=quality??s.Quality,FrameRate=frameRate??s.FrameRate,Scale=scale??s.Scale,TrimStart=trimStart??s.TrimStart,TrimEnd=trimEnd??s.TrimEnd,HasAudio=s.HasAudio,MuteAudio=s.MuteAudio,SourceWidth=s.SourceWidth,SourceHeight=s.SourceHeight,SourceDuration=s.SourceDuration};
}
