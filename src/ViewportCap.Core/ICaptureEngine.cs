namespace ViewportCap.Core;
public interface ICaptureEngine{event EventHandler<CaptureFailedEventArgs>? Failed;Task StartAsync(CaptureRequest request,CancellationToken cancellationToken);Task<CaptureResult> StopAsync(CancellationToken cancellationToken);Task CancelAsync(bool deleteFile=true);}
