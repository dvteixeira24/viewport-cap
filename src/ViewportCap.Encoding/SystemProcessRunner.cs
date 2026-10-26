using System.Diagnostics;
using System.Text;
namespace ViewportCap.Encoding;
public sealed class SystemProcessRunner:IProcessRunner
{
 public async Task<ProcessResult> RunAsync(string fileName,IReadOnlyList<string> arguments,Action<string>? onStandardOutput,Action<string>? onStandardError,CancellationToken cancellationToken)
 {
  var info=new ProcessStartInfo{FileName=fileName,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8,StandardErrorEncoding=System.Text.Encoding.UTF8};foreach(var arg in arguments)info.ArgumentList.Add(arg);
  using var process=new Process{StartInfo=info};if(!process.Start())throw new InvalidOperationException($"Could not start {fileName}.");var stdout=ReadAllAsync(process.StandardOutput,onStandardOutput);var stderr=ReadAllAsync(process.StandardError,onStandardError);
  try{await process.WaitForExitAsync(cancellationToken);}catch(OperationCanceledException){try{if(!process.HasExited)process.Kill(true);}catch{}throw;}
  return new ProcessResult(process.ExitCode,await stdout,await stderr);
 }
 private static async Task<string> ReadAllAsync(StreamReader reader,Action<string>? cb){var b=new StringBuilder();string? line;while((line=await reader.ReadLineAsync())!=null){cb?.Invoke(line);b.AppendLine(line);}return b.ToString();}
}
