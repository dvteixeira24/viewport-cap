using System.Globalization;
using ViewportCap.Core;
namespace ViewportCap.Encoding;
public static class FfmpegArgumentBuilder
{
 public static IReadOnlyList<string> Build(ReviewSettings s,string outputPath,FfmpegCapabilities caps)
 {
  if(s.Format==OutputFormat.Mp4&&!caps.CanEncodeH264)throw new InvalidOperationException("No H.264 encoder is available in FFmpeg.");if(s.Format==OutputFormat.WebM&&!caps.HasVp9)throw new InvalidOperationException("libvpx-vp9 is not available in FFmpeg.");
  var a=new List<string>{"-y","-hide_banner","-nostdin","-i",s.SourcePath,"-ss",FormatTime(s.TrimStart),"-t",FormatTime(s.TrimDuration)};var audio=s.Format!=OutputFormat.Gif&&s.HasAudio&&!s.MuteAudio;
  if(s.Format!=OutputFormat.Gif){a.Add("-map");a.Add("0:v:0");}if(audio){a.Add("-map");a.Add("0:a:0?");}else a.Add("-an");var (w,h)=Size(s.SourceWidth,s.SourceHeight,s.Scale);
  switch(s.Format){case OutputFormat.Mp4:AddMp4(a,s,caps,w,h,audio);break;case OutputFormat.WebM:AddWebm(a,s,w,h,audio);break;case OutputFormat.Gif:AddGif(a,s,w,h);break;default:throw new ArgumentOutOfRangeException(nameof(s));}
  a.AddRange(new[]{"-progress","pipe:1","-nostats",outputPath});return a;
 }
 private static void AddMp4(List<string>a,ReviewSettings s,FfmpegCapabilities c,int w,int h,bool audio){var enc=c.HasH264Mf?"h264_mf":"libopenh264";var bpp=s.Quality switch{QualityPreset.High=>.16,QualityPreset.Small=>.08,_=>.12};var br=(int)Math.Clamp(w*(double)h*s.FrameRate*bpp,2_000_000,40_000_000);a.AddRange(new[]{"-vf",VideoFilter(s,w,h),"-c:v",enc,"-b:v",br.ToString(CultureInfo.InvariantCulture),"-pix_fmt","yuv420p","-movflags","+faststart"});if(audio)AddAudio(a,"aac","128k");}
 private static void AddWebm(List<string>a,ReviewSettings s,int w,int h,bool audio){var crf=s.Quality switch{QualityPreset.High=>"24",QualityPreset.Small=>"40",_=>"32"};a.AddRange(new[]{"-vf",VideoFilter(s,w,h),"-c:v","libvpx-vp9","-b:v","0","-crf",crf,"-row-mt","1","-pix_fmt","yuv420p"});if(audio)AddAudio(a,"libopus","96k");}
 private static void AddGif(List<string>a,ReviewSettings s,int w,int h){var colors=s.Quality switch{QualityPreset.High=>256,QualityPreset.Small=>64,_=>128};var scale=w!=s.SourceWidth||h!=s.SourceHeight?$"scale={w}:{h}:flags=lanczos,":"";var filter=$"[0:v]fps={s.FrameRate},{scale}split[a][b];[a]palettegen=max_colors={colors}:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=3:diff_mode=rectangle";a.AddRange(new[]{"-filter_complex",filter,"-loop","0"});}
 private static void AddAudio(List<string>a,string enc,string rate){a.AddRange(new[]{"-c:a",enc,"-b:a",rate});}
 private static string VideoFilter(ReviewSettings s,int w,int h){var scale=w!=s.SourceWidth||h!=s.SourceHeight?$",scale={w}:{h}:flags=lanczos":"";return $"fps={s.FrameRate}{scale}";}
 private static (int Width,int Height) Size(int sw,int sh,ScalePreset p){var f=p switch{ScalePreset.Percent75=>.75,ScalePreset.Percent50=>.5,ScalePreset.Fit960=>Math.Min(1,960.0/Math.Max(1,sw)),_=>1.0};var w=Math.Max(2,(int)Math.Round(sw*f));var h=Math.Max(2,(int)Math.Round(sh*f));if(w%2!=0)w--;if(h%2!=0)h--;return(w,h);}
 private static string FormatTime(TimeSpan t)=>t.ToString(@"hh\:mm\:ss\.fff",CultureInfo.InvariantCulture);
}
