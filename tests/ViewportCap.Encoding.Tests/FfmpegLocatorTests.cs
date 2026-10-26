using ViewportCap.Encoding;using Xunit;
namespace ViewportCap.Encoding.Tests;
public sealed class FfmpegLocatorTests:IDisposable
{
 private readonly string dir=Path.Combine(Path.GetTempPath(),"viewport-cap-tests",Guid.NewGuid().ToString("N"));
 public void Dispose(){if(Directory.Exists(dir))Directory.Delete(dir,true);}
 [Fact] public void Find_ReturnsFfmpegFromExtraDirectory(){Directory.CreateDirectory(dir);var p=Path.Combine(dir,"ffmpeg.exe");File.WriteAllText(p,"fake");Assert.Equal(p,FfmpegLocator.Find(dir));}
 [Fact] public void Find_ReturnsNullWhenMissing()=>Assert.Null(FfmpegLocator.Find(dir));
 [Fact] public void Find_IgnoresPlantedWorkingDirectoryCopy()
 {
  var previous=Environment.CurrentDirectory;
  try
  {
   var plantDir=Path.Combine(dir,"tools","ffmpeg");
   Directory.CreateDirectory(plantDir);
   var planted=Path.Combine(plantDir,"ffmpeg.exe");
   File.WriteAllText(planted,"fake");
   Environment.CurrentDirectory=dir;
   Assert.NotEqual(planted,FfmpegLocator.Find());
  }
  finally{Environment.CurrentDirectory=previous;}
 }
}
