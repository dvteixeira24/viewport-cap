using ViewportCap.Core;
namespace ViewportCap.Encoding;
public sealed class FfmpegExporter:IExporter
{
 private readonly string _ffmpegPath;private readonly FfmpegCapabilities _capabilities;private readonly IProcessRunner _runner;
 public FfmpegExporter(string ffmpegPath,FfmpegCapabilities capabilities,IProcessRunner runner){_ffmpegPath=ffmpegPath;_capabilities=capabilities;_runner=runner;}
 public async Task<ExportResult> ExportAsync(ExportPlan plan,IProgress<double>? progress,CancellationToken cancellationToken)
 {
  var dir=Path.GetDirectoryName(plan.OutputPath);if(string.IsNullOrWhiteSpace(dir))throw new ExportException("The output path has no directory.");Directory.CreateDirectory(dir);if(!File.Exists(plan.Settings.SourcePath))throw new ExportException("The temporary recording is missing.");
  if(plan.CopySource){ExportFiles.CopyIntoPlace(plan.Settings.SourcePath,plan.OutputPath);return new(plan.OutputPath);}
  var partial=ExportFiles.PartialPath(plan.OutputPath);if(File.Exists(partial))File.Delete(partial);var args=FfmpegArgumentBuilder.Build(plan.Settings,partial,_capabilities);var parser=new FfmpegProgressParser(plan.Settings.TrimDuration.TotalSeconds);
  try{var result=await _runner.RunAsync(_ffmpegPath,args,line=>{parser.Feed(line);if(parser.Percent is double p)progress?.Report(p);},null,cancellationToken);if(result.ExitCode!=0)throw new ExportException($"FFmpeg failed: {Tail(result.StandardError)}");if(!File.Exists(partial))throw new ExportException("FFmpeg did not produce an output file.");File.Move(partial,plan.OutputPath,true);return new(plan.OutputPath);}
  catch{try{if(File.Exists(partial))File.Delete(partial);}catch{}throw;}
 }
 private static string Tail(string text)=>text.Length<=2000?text:text[^2000..];
}
