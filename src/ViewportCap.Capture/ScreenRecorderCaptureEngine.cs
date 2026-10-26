using System.Diagnostics;
using ScreenRecorderLib;
using ViewportCap.Core;
namespace ViewportCap.Capture;
public sealed class ScreenRecorderCaptureEngine:ICaptureEngine
{
 private readonly object _gate=new();private Recorder? _recorder;private TaskCompletionSource<CaptureResult>? _completion;private string? _outputPath;private bool _hasAudio;private int _width,_height;private Stopwatch? _stopwatch;
 public event EventHandler<CaptureFailedEventArgs>? Failed;
 public Task StartAsync(CaptureRequest request,CancellationToken cancellationToken)
 {
  cancellationToken.ThrowIfCancellationRequested();lock(_gate){if(_recorder!=null)throw new InvalidOperationException("A recording is already active.");if(!request.Region.IsValid)throw new CaptureException("The selected region is too small.");var dir=Path.GetDirectoryName(request.OutputPath);if(string.IsNullOrWhiteSpace(dir))throw new CaptureException("The recording path has no directory.");Directory.CreateDirectory(dir);if(File.Exists(request.OutputPath))File.Delete(request.OutputPath);
   var completion=new TaskCompletionSource<CaptureResult>(TaskCreationOptions.RunContinuationsAsynchronously);var recorder=Recorder.CreateRecorder(ScreenRecorderOptionsFactory.Create(request));recorder.OnRecordingComplete+=OnRecordingComplete;recorder.OnRecordingFailed+=OnRecordingFailed;_recorder=recorder;_completion=completion;_outputPath=request.OutputPath;_hasAudio=request.AudioMode!=AudioMode.Off;_width=request.Region.Width;_height=request.Region.Height;_stopwatch=Stopwatch.StartNew();try{recorder.Record(request.OutputPath);}catch{CleanupAfterStartFailure();throw;}}
  return Task.CompletedTask;
 }
 public async Task<CaptureResult> StopAsync(CancellationToken cancellationToken){TaskCompletionSource<CaptureResult> c;lock(_gate){if(_recorder==null||_completion==null)throw new InvalidOperationException("No recording is active.");c=_completion;_stopwatch?.Stop();try{_recorder.Stop();}catch(Exception ex){c.TrySetException(new CaptureException("Could not stop the recording.",ex));}}try{return await c.Task.WaitAsync(TimeSpan.FromSeconds(30),cancellationToken);}finally{DisposeRecorder();}}
 public async Task CancelAsync(bool deleteFile=true){string? path;TaskCompletionSource<CaptureResult>? c;lock(_gate){path=_outputPath;c=_completion;try{_recorder?.Stop();}catch{}}if(c!=null){try{await c.Task.WaitAsync(TimeSpan.FromSeconds(5));}catch{}}DisposeRecorder();if(deleteFile&&!string.IsNullOrWhiteSpace(path)&&File.Exists(path)){try{File.Delete(path);}catch{}}}
 private void OnRecordingComplete(object? sender,RecordingCompleteEventArgs e){var r=new CaptureResult(e.FilePath??_outputPath??string.Empty,_hasAudio,_stopwatch?.Elapsed??TimeSpan.Zero,_width,_height);_completion?.TrySetResult(r);}
 private void OnRecordingFailed(object? sender,RecordingFailedEventArgs e){_completion?.TrySetException(new CaptureException(e.Error));Failed?.Invoke(this,new CaptureFailedEventArgs(e.Error));}
 private void DisposeRecorder(){lock(_gate){if(_recorder!=null){_recorder.OnRecordingComplete-=OnRecordingComplete;_recorder.OnRecordingFailed-=OnRecordingFailed;_recorder.Dispose();}_recorder=null;_completion=null;_outputPath=null;_width=0;_height=0;_stopwatch=null;}}
 private void CleanupAfterStartFailure()=>DisposeRecorder();
}
